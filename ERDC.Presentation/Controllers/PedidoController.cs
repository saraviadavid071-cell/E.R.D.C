using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ERDC.Application.DTOs;
using ERDC.Application.Interfaces;
using ERDC.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERDC.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        private readonly ICotizacionService _cotizacionService;
        private readonly IPedidoService _pedidoService;
        private readonly IApplicationDbContext _context;

        public PedidoController(
            ICotizacionService cotizacionService,
            IPedidoService pedidoService,
            IApplicationDbContext context)
        {
            _cotizacionService = cotizacionService;
            _pedidoService = pedidoService;
            _context = context;
        }

        // 1. Procesar Compra o Cotización
        [HttpPost("procesar")]
        public async Task<IActionResult> ProcesarCompra([FromBody] CrearPedidoDto pedidoDto)
        {
            try
            {
                var pedido = await _pedidoService.RegistrarPedidoOCotizacionAsync(pedidoDto);
                return Ok(new
                {
                    mensaje = pedido.TipoTransaccion == "venta"
                        ? "Orden de compra procesada y stock actualizado exitosamente."
                        : "Cotización registrada y lista para descargar en PDF.",
                    idPedido = pedido.IdPedido,
                    montoSubtotal = pedido.MontoSubtotal,
                    montoDescuento = pedido.MontoDescuento,
                    costoEnvio = pedido.CostoEnvio,
                    montoIva = pedido.MontoIva,
                    montoTotal = pedido.MontoTotal
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // 2. Descargar Cotización en PDF
        [HttpGet("{idPedido:int}/cotizacion-pdf")]
        [HttpPost("{idPedido:int}/cotizacion-pdf")]
        public async Task<FileStreamResult> DescargarCotizacionPDF(int idPedido)
        {
            byte[] pdfBytes = await _cotizacionService.GenerarCotizacionPdf(idPedido);
            var stream = new MemoryStream(pdfBytes);
            return new FileStreamResult(stream, "application/pdf")
            {
                FileDownloadName = $"Cotizacion_ERDC_{idPedido:D5}.pdf"
            };
        }

        // 3. Consultar Catálogo de Productos para React
        [HttpGet("/api/catalogo")]
        public async Task<IActionResult> ObtenerCatalogo([FromQuery] string? categoria = null, [FromQuery] string? buscar = null)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Inventario)
                .Where(p => p.Estado == "activo")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todos los productos")
                query = query.Where(p => p.Categoria != null && p.Categoria.Nombre == categoria);

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(p => p.Nombre.Contains(buscar) || p.Sku.Contains(buscar));

            var productos = await query
                .Select(p => new
                {
                    idProducto = p.IdProducto,
                    sku = p.Sku,
                    nombre = p.Nombre,
                    descripcion = p.Descripcion,
                    precio = p.Precio,
                    categoria = p.Categoria != null ? p.Categoria.Nombre : "General",
                    stock = p.Inventario != null ? p.Inventario.Stock : 0,
                    stockCritico = p.Inventario != null && p.Inventario.Stock <= 5,
                    imagenUrl = p.ImagenUrl
                })
                .ToListAsync();

            return Ok(productos);
        }

        // 4. Validar Cupón de Descuento en el Carrito
        [HttpGet("/api/cupones/validar/{codigo}")]
        public async Task<IActionResult> ValidarCupon(string codigo)
        {
            string limpio = codigo.Trim().ToUpperInvariant();
            var cupon = await _context.Cupones
                .FirstOrDefaultAsync(c => c.Codigo == limpio && c.Activo &&
                    (c.FechaExpiracion == null || c.FechaExpiracion >= DateTime.Now));

            if (cupon == null)
                return NotFound(new { error = "El cupón ingresado no existe o ha expirado." });

            return Ok(new
            {
                codigo = cupon.Codigo,
                porcentajeDescuento = cupon.PorcentajeDescuento
            });
        }

        // 5. Guardar Nuevo Utensilio desde el Panel Admin
        [HttpPost("/api/admin/productos")]
        public async Task<IActionResult> GuardarNuevoInsumo([FromBody] NuevoProductoDto dto)
        {
            var producto = new Producto
            {
                IdCategoria = dto.IdCategoria,
                Sku = dto.Sku,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion ?? "Utensilio profesional de coctelería",
                Precio = dto.Precio,
                ImagenUrl = dto.ImagenUrl,
                Estado = "activo"
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            var inventario = new Inventario
            {
                IdProducto = producto.IdProducto,
                Stock = dto.StockInicial
            };

            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Insumo registrado en catálogo y almacén.", idProducto = producto.IdProducto });
        }

        // 6. Cargar Stock desde el Panel Admin
        [HttpPatch("/api/admin/inventario/{idProducto:int}/cargar-stock")]
        public async Task<IActionResult> CargarStockManual(int idProducto, [FromQuery] int unidades)
        {
            if (unidades <= 0)
                return BadRequest(new { error = "La cantidad a cargar debe ser mayor a cero." });

            var inv = await _context.Inventarios.FirstOrDefaultAsync(i => i.IdProducto == idProducto);
            if (inv == null)
                return NotFound(new { error = "Producto no encontrado en inventario." });

            inv.Stock += unidades;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Stock actualizado.", nuevoStock = inv.Stock, stockCritico = inv.EsStockCritico() });
        }
    }
}
