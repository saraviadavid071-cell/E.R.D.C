using System;
using System.Collections.Generic;
using System.Text;

namespace ERDC.Infrastructure.Persistence
{
    using ERDC.Application.Interfaces;
    using ERDC.Domain.Entities;
    using Microsoft.EntityFrameworkCore;

    // ====================================================================
    // CAPA 3 (Infraestructura): ApplicationDbContext
    // Exactamente como aparece en tu Diagrama de Clases y de Secuencia
    // Ubicación: ERDC.Infrastructure/Persistence/ApplicationDbContext.cs
    // ====================================================================
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Direccion> Direcciones => Set<Direccion>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Inventario> Inventarios => Set<Inventario>();
        public DbSet<Cupon> Cupones => Set<Cupon>();
        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<DetallePedido> DetallesPedidos => Set<DetallePedido>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Inventario)
                .WithOne(i => i.Producto)
                .HasForeignKey<Inventario>(i => i.IdProducto)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Cupon>()
                .HasIndex(c => c.Codigo)
                .IsUnique();

            // Evitar ciclos de borrado en cascada múltiple en SQL Server (Error 1785)
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Direccion)
                .WithMany()
                .HasForeignKey(p => p.IdDireccion)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
