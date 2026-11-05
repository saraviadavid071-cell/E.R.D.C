using System;
using System.Collections.Generic;
using System.Text;

namespace ERDC.Application.DTOs
{
    // ====================================================================
    // DTOs de Entrada (Exactamente como pide PedidoController en el Diagrama de Clases)
    // Ubicación: ERDC.Application/DTOs/CrearPedidoDto.cs
    // ====================================================================
    public class ItemCarritoDto
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
    }

    public class CrearPedidoDto
    {
        public int IdUsuario { get; set; } = 2; // Cliente por defecto o autenticado
        public string NombreEmpresa { get; set; } = string.Empty;
        public string CedulaRuc { get; set; } = string.Empty;
        public string DireccionCalle { get; set; } = string.Empty;
        public string ReferenciasLocales { get; set; } = string.Empty;
        public string Municipio { get; set; } = "Managua";
        public string Departamento { get; set; } = "Managua";
        public string Telefono { get; set; } = "+505 8888-0000";
        public string? NotasEspeciales { get; set; }
        public string? CodigoCupon { get; set; }
        public string TipoTransaccion { get; set; } = "cotizacion"; // "venta" (CU05) o "cotizacion" (CU06)
        public string MetodoPago { get; set; } = "cotizacion_pdf";
        public List<ItemCarritoDto> Items { get; set; } = new();
    }

    public class NuevoProductoDto
    {
        public int IdCategoria { get; set; } = 1;
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int StockInicial { get; set; }
        public string? ImagenUrl { get; set; }
    }
}
