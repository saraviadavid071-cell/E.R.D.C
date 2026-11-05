using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERDC.Application.DTOs;
using ERDC.Application.Interfaces;
using ERDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERDC.Application.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IApplicationDbContext _context;

        public PedidoService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> VerificarDisponibilidad(List<ItemCarritoDto> items)
        {
            if (items == null || !items.Any())
                return false;

            foreach (var item in items)
            {
                var inventario = await _context.Inventarios
                    .FirstOrDefaultAsync(i => i.IdProducto == item.IdProducto);

                if (inventario == null || inventario.Stock < item.Cantidad || item.Cantidad <= 0)
                    return false;
            }
            return true;
        }

        public async Task DescontarStock(List<ItemCarritoDto> items)
        {
            foreach (var item in items)
            {
                var inventario = await _context.Inventarios
                    .Include(i => i.Producto)
                    .FirstOrDefaultAsync(i => i.IdProducto == item.IdProducto);

                if (inventario == null || inventario.Stock < item.Cantidad)
                {
                    string nombreProd = inventario?.Producto?.Nombre ?? $"ID {item.IdProducto}";
                    throw new InvalidOperationException(
                        $"Stock insuficiente para el artículo '{nombreProd}'. Disponible: {inventario?.Stock ?? 0} uds.");
                }

                inventario.Stock -= item.Cantidad;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Pedido> RegistrarPedidoOCotizacionAsync(CrearPedidoDto dto)
        {
            bool hayStock = await VerificarDisponibilidad(dto.Items);
            if (!hayStock)
                throw new InvalidOperationException("Uno o más productos no tienen stock suficiente en tiempo real.");

            var direccion = new Direccion
            {
                IdUsuario = dto.IdUsuario,
                TipoDireccion = "envio",
                NombreEmpresa = dto.NombreEmpresa,
                CedulaRuc = dto.CedulaRuc,
                DireccionLinea = dto.DireccionCalle,
                Referencias = dto.ReferenciasLocales,
                Municipio = string.IsNullOrWhiteSpace(dto.Municipio) ? "Managua" : dto.Municipio,
                Departamento = string.IsNullOrWhiteSpace(dto.Departamento) ? "Managua" : dto.Departamento,
                Telefono = dto.Telefono,
                Observaciones = dto.NotasEspeciales
            };

            _context.Direcciones.Add(direccion);
            await _context.SaveChangesAsync();

            Cupon? cuponValido = null;
            decimal porcentajeDescuento = 0m;
            if (!string.IsNullOrWhiteSpace(dto.CodigoCupon))
            {
                string codigoLimpio = dto.CodigoCupon.Trim().ToUpperInvariant();
                cuponValido = await _context.Cupones
                    .FirstOrDefaultAsync(c => c.Codigo == codigoLimpio && c.Activo &&
                        (c.FechaExpiracion == null || c.FechaExpiracion >= DateTime.Now));

                if (cuponValido != null)
                    porcentajeDescuento = cuponValido.PorcentajeDescuento;
            }

            var pedido = new Pedido
            {
                IdUsuario = dto.IdUsuario,
                IdDireccion = direccion.IdDireccion,
                IdCupon = cuponValido?.IdCupon,
                TipoTransaccion = dto.TipoTransaccion == "venta" ? "venta" : "cotizacion",
                MetodoPago = dto.MetodoPago,
                Estado = dto.TipoTransaccion == "venta" ? "pagado" : "cotizacion_emitida",
                NotasPedido = dto.NotasEspeciales,
                FechaPedido = DateTime.Now
            };

            foreach (var item in dto.Items)
            {
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto)
                    ?? throw new InvalidOperationException($"El producto con ID {item.IdProducto} no existe.");

                pedido.Detalles.Add(new DetallePedido
                {
                    IdProducto = producto.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = producto.Precio
                });
            }

            pedido.CalcularTotalPedido(porcentajeDescuento);

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            if (pedido.TipoTransaccion == "venta")
                await DescontarStock(dto.Items);

            return pedido;
        }
    }
}
