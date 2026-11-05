using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERDC.Domain.Entities
{
    // ====================================================================
    // CAPA 1: E.R.D.C. Domain
    // Implementación exacta del Diagrama de Clases UML (Sección 2.1.3)
    // y mapeo 1:1 con las tablas de SQL Server (ERDC_Corregido.sql)
    // ====================================================================

    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required, MaxLength(50)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        [Column("apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        [Column("nombre_visible")]
        public string NombreVisible { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        [Column("rol")]
        public string Rol { get; set; } = "cliente";

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Propiedades de navegación
        public ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }

    [Table("direcciones")]
    public class Direccion
    {
        [Key]
        [Column("id_direccion")]
        public int IdDireccion { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required, MaxLength(20)]
        [Column("tipo_direccion")]
        public string TipoDireccion { get; set; } = "envio";

        [MaxLength(30)]
        [Column("cedula_ruc")]
        public string? CedulaRuc { get; set; }

        [MaxLength(150)]
        [Column("nombre_empresa")]
        public string? NombreEmpresa { get; set; }

        [Required, MaxLength(255)]
        [Column("direccion_linea")]
        public string DireccionLinea { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("referencias")]
        public string? Referencias { get; set; }

        [MaxLength(20)]
        [Column("codigo_postal")]
        public string? CodigoPostal { get; set; } = "11001";

        [Required, MaxLength(100)]
        [Column("municipio")]
        public string Municipio { get; set; } = "Managua";

        [Required, MaxLength(100)]
        [Column("departamento")]
        public string Departamento { get; set; } = "Managua";

        [Required, MaxLength(20)]
        [Column("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }
    }

    [Table("categorias")]
    public class Categoria
    {
        [Key]
        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        [Required, MaxLength(100)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        // Navegación
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    [Table("productos")]
    public class Producto
    {
        [Key]
        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        [Required, MaxLength(50)]
        [Column("sku")]
        public string Sku { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("precio", TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        [MaxLength(255)]
        [Column("imagen_url")]
        public string? ImagenUrl { get; set; }

        [Required, MaxLength(20)]
        [Column("estado")]
        public string Estado { get; set; } = "activo";

        // Navegación
        [ForeignKey(nameof(IdCategoria))]
        public Categoria? Categoria { get; set; }

        public Inventario? Inventario { get; set; }

        public string ObtenerFichaTecnica()
        {
            return $"[{Sku}] {Nombre} — {Descripcion ?? "Sin especificación técnica"} (C$ {Precio:N2})";
        }
    }

    [Table("inventarios")]
    public class Inventario
    {
        [Key]
        [Column("id_inventario")]
        public int IdInventario { get; set; }

        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Column("stock")]
        public int Stock { get; set; }

        [ForeignKey(nameof(IdProducto))]
        public Producto? Producto { get; set; }

        public bool EsStockCritico() => Stock <= 5;
    }

    [Table("cupones")]
    public class Cupon
    {
        [Key]
        [Column("id_cupon")]
        public int IdCupon { get; set; }

        [Required, MaxLength(30)]
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [Column("porcentaje_descuento", TypeName = "decimal(5,2)")]
        public decimal PorcentajeDescuento { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("fecha_expiracion")]
        public DateTime? FechaExpiracion { get; set; }
    }

    [Table("pedidos")]
    public class Pedido
    {
        [Key]
        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("id_direccion")]
        public int IdDireccion { get; set; }

        [Column("id_cupon")]
        public int? IdCupon { get; set; }

        [Column("monto_subtotal", TypeName = "decimal(10,2)")]
        public decimal MontoSubtotal { get; set; }

        [Column("monto_descuento", TypeName = "decimal(10,2)")]
        public decimal MontoDescuento { get; set; } = 0.00m;

        [Column("costo_envio", TypeName = "decimal(10,2)")]
        public decimal CostoEnvio { get; set; } = 150.00m;

        [Column("monto_iva", TypeName = "decimal(10,2)")]
        public decimal MontoIva { get; set; }

        [Column("monto_total", TypeName = "decimal(10,2)")]
        public decimal MontoTotal { get; set; }

        [Required, MaxLength(50)]
        [Column("metodo_pago")]
        public string MetodoPago { get; set; } = "por_definir";

        [Required, MaxLength(30)]
        [Column("tipo_transaccion")]
        public string TipoTransaccion { get; set; } = "cotizacion";

        [Required, MaxLength(50)]
        [Column("estado")]
        public string Estado { get; set; } = "pendiente";

        [Column("notas_pedido")]
        public string? NotasPedido { get; set; }

        [Column("fecha_pedido")]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }

        [ForeignKey(nameof(IdDireccion))]
        public Direccion? Direccion { get; set; }

        [ForeignKey(nameof(IdCupon))]
        public Cupon? Cupon { get; set; }

        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();

        public void CalcularTotalPedido(decimal porcentajeDescuento = 0m)
        {
            decimal sumaRenglones = 0m;
            foreach (var item in Detalles)
            {
                sumaRenglones += item.Cantidad * item.PrecioUnitario;
            }

            MontoSubtotal = Math.Round(sumaRenglones, 2);
            MontoDescuento = Math.Round(MontoSubtotal * (porcentajeDescuento / 100m), 2);
            decimal baseImponible = MontoSubtotal - MontoDescuento;

            MontoIva = Math.Round(baseImponible * 0.15m, 2);
            CostoEnvio = 150.00m;
            MontoTotal = Math.Round(baseImponible + MontoIva + CostoEnvio, 2);
        }
    }

    [Table("detalle_pedidos")]
    public class DetallePedido
    {
        [Key]
        [Column("id_detalle")]
        public int IdDetalle { get; set; }

        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_unitario", TypeName = "decimal(10,2)")]
        public decimal PrecioUnitario { get; set; }

        [ForeignKey(nameof(IdPedido))]
        public Pedido? Pedido { get; set; }

        [ForeignKey(nameof(IdProducto))]
        public Producto? Producto { get; set; }
    }
}
