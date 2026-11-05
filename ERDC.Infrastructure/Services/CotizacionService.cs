using ERDC.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace ERDC.Infrastructure.Services
{
    public class CotizacionService : ICotizacionService
    {
        private readonly IApplicationDbContext _context;

        public CotizacionService(IApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerarCotizacionPdf(int idPedido)
        {
            // 1. Consulta el pedido y sus productos en SQL Server
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Direccion)
                .Include(p => p.Cupon)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.IdPedido == idPedido);

            if (pedido == null)
                throw new InvalidOperationException($"No se encontró la cotización #{idPedido}.");

            // 2. Verifica que siga habiendo stock antes de armar el PDF (Flujo Alterno CU06)
            foreach (var det in pedido.Detalles)
            {
                var inv = await _context.Inventarios
                    .FirstOrDefaultAsync(i => i.IdProducto == det.IdProducto);

                if (inv == null || inv.Stock < det.Cantidad)
                    throw new InvalidOperationException($"El artículo '{det.Producto?.Nombre}' se quedó sin stock.");
            }

            DateTime fechaVigencia = pedido.FechaPedido.AddDays(15);

            // 3. Arma el diseño del PDF con el logo de E.R.D.C. y precios en Córdobas (C$)
            var documento = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor("#1E293B"));

                    // Cabecera del PDF
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("E.R.D.C.").FontSize(22).Bold().FontColor("#991B1B");
                                left.Item().Text("EL RINCÓN DEL COCKTAIL · SUMINISTROS PARA BAR").FontSize(9).Bold().FontColor("#475569");
                                left.Item().Text("Managua, Nicaragua · contacto@erdc.com.ni").FontSize(8).FontColor("#64748B");
                            });

                            row.ConstantItem(190).Background("#F8FAFC").Border(1).BorderColor("#CBD5E1").Padding(8).Column(right =>
                            {
                                right.Item().AlignRight().Text("COTIZACIÓN FORMAL").FontSize(11).Bold().FontColor("#991B1B");
                                right.Item().AlignRight().Text($"N° COT-{pedido.IdPedido:D5}").FontSize(12).Bold();
                                right.Item().AlignRight().Text($"Emisión: {pedido.FechaPedido:dd/MM/yyyy}").FontSize(8);
                                right.Item().AlignRight().Text($"Vigencia: {fechaVigencia:dd/MM/yyyy}").FontSize(8).Bold().FontColor("#166534");
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor("#991B1B");
                    });

                    // Cuerpo del PDF
                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Item().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("DATOS DEL BAR / EMPRESA").FontSize(8).Bold().FontColor("#64748B");
                                c.Item().PaddingTop(2).Text($"Establecimiento: {pedido.Direccion?.NombreEmpresa ?? "Cliente"}").Bold();
                                c.Item().Text($"Cédula / RUC: {pedido.Direccion?.CedulaRuc ?? "N/A"}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("DIRECCIÓN DE ENTREGA (NICARAGUA)").FontSize(8).Bold().FontColor("#64748B");
                                c.Item().PaddingTop(2).Text($"{pedido.Direccion?.DireccionLinea}");
                                c.Item().Text($"Referencias: {pedido.Direccion?.Referencias ?? "Sin referencias"}");
                                c.Item().Text($"{pedido.Direccion?.Municipio}, {pedido.Direccion?.Departamento}");
                            });
                        });

                        col.Item().PaddingTop(14);

                        // Tabla de Productos
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(75);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(55);
                                columns.ConstantColumn(85);
                                columns.ConstantColumn(90);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#1E293B").Padding(6).Text("SKU").FontColor(Colors.White).Bold().FontSize(9);
                                header.Cell().Background("#1E293B").Padding(6).Text("PRODUCTO").FontColor(Colors.White).Bold().FontSize(9);
                                header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("CANT.").FontColor(Colors.White).Bold().FontSize(9);
                                header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("PRECIO UNIT.").FontColor(Colors.White).Bold().FontSize(9);
                                header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("SUBTOTAL").FontColor(Colors.White).Bold().FontSize(9);
                            });

                            foreach (var item in pedido.Detalles)
                            {
                                decimal subRenglon = item.Cantidad * item.PrecioUnitario;
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).Text(item.Producto?.Sku ?? "-").FontSize(9).Bold();
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).Text(item.Producto?.Nombre ?? "Utensilio").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text($"{item.Cantidad}");
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text($"C$ {item.PrecioUnitario:N2}");
                                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text($"C$ {subRenglon:N2}").Bold();
                            }
                        });

                        // Totales (Subtotal, Envío C$ 150, IVA 15%, Total)
                        col.Item().PaddingTop(14).AlignRight().Width(220).Background("#F8FAFC").Border(1).BorderColor("#CBD5E1").Padding(10).Column(tot =>
                        {
                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:");
                                r.ConstantItem(90).AlignRight().Text($"C$ {pedido.MontoSubtotal:N2}").Bold();
                            });

                            if (pedido.MontoDescuento > 0)
                            {
                                tot.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("Descuento:").FontColor("#166534");
                                    r.ConstantItem(90).AlignRight().Text($"- C$ {pedido.MontoDescuento:N2}").FontColor("#166534").Bold();
                                });
                            }

                            tot.Item().PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Text("Envío Fijo:");
                                r.ConstantItem(90).AlignRight().Text($"C$ {pedido.CostoEnvio:N2}");
                            });

                            tot.Item().PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Text("IVA (15%):");
                                r.ConstantItem(90).AlignRight().Text($"C$ {pedido.MontoIva:N2}");
                            });

                            tot.Item().PaddingVertical(5).LineHorizontal(1).LineColor("#CBD5E1");

                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL:").FontSize(11).Bold().FontColor("#991B1B");
                                r.ConstantItem(100).AlignRight().Text($"C$ {pedido.MontoTotal:N2}").FontSize(12).Bold().FontColor("#991B1B");
                            });
                        });
                    });

                    // Pie de página
                    page.Footer().AlignCenter().Text("E.R.D.C. — El Rincón del Cocktail · Managua, Nicaragua").FontSize(8).FontColor("#64748B");
                });
            });

            return documento.GeneratePdf();
        }
    }
}
