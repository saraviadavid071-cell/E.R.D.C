using ERDC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERDC.Application.Interfaces
{
    // ====================================================================
    // CAPA 2 (Interfaz de Persistencia): IApplicationDbContext
    // Exactamente como aparece en tu Diagrama de Clases UML (Sección 2.1.3)
    // Ubicación: ERDC.Application/Interfaces/IApplicationDbContext.cs
    // ====================================================================
    public interface IApplicationDbContext
    {
        DbSet<Usuario> Usuarios { get; }
        DbSet<Direccion> Direcciones { get; }
        DbSet<Categoria> Categorias { get; }
        DbSet<Producto> Productos { get; }
        DbSet<Inventario> Inventarios { get; }
        DbSet<Cupon> Cupones { get; }
        DbSet<Pedido> Pedidos { get; }
        DbSet<DetallePedido> DetallesPedidos { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
