using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Freela.Models;

namespace Freela.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Cada DbSet é uma tabela. O nome precisa ser igual ao da tabela no banco.
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Freelancer> Freelancers { get; set; }

        // vagas (projetos) e suas tabelas de apoio
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Habilidade> Habilidades { get; set; }
        public DbSet<Projeto> Projetos { get; set; }

        //candidaturas, interesses e matches
        public DbSet<Candidatura> Candidaturas { get; set; }
        public DbSet<Interesse> Interesses { get; set; }
        public DbSet<Match> Matches { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Usuario <-> Cliente (1:1, o Id do Cliente é o mesmo Id do Usuario)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Cliente)
                .WithOne(c => c.Usuario)
                .HasForeignKey<Cliente>(c => c.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Usuario <-> Freelancer (1:1)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Freelancer)
                .WithOne(f => f.Usuario)
                .HasForeignKey<Freelancer>(f => f.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // O Id vem do Usuario, o banco não gera sozinho
            modelBuilder.Entity<Cliente>().Property(c => c.Id).ValueGeneratedNever();
            modelBuilder.Entity<Freelancer>().Property(f => f.Id).ValueGeneratedNever();

            // DataCadastro: se não informarmos, o banco preenche (CURRENT_TIMESTAMP)
            modelBuilder.Entity<Usuario>()
                .Property(u => u.DataCadastro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Nomes únicos, como no banco (UNIQUE)
            modelBuilder.Entity<Categoria>().HasIndex(c => c.Nome).IsUnique();
            modelBuilder.Entity<Habilidade>().HasIndex(h => h.Nome).IsUnique();

            // ---------- PROJETO (vaga)----------

            // No banco, as FKs de Projetos NÃO têm ON DELETE CASCADE: Restrict reflete isso
            // (uma empresa com vagas não pode ser apagada sem antes tratar as vagas)
            modelBuilder.Entity<Projeto>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Projeto>()
                .HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Projeto N:M Habilidade, pela tabela ProjetoHabilidades (ProjetoId, HabilidadeId)
            modelBuilder.Entity<Projeto>()
                .HasMany(p => p.Habilidades)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "ProjetoHabilidades",
                    lado => lado.HasOne<Habilidade>().WithMany().HasForeignKey("HabilidadeId"),
                    outroLado => outroLado.HasOne<Projeto>().WithMany().HasForeignKey("ProjetoId"),
                    tabela => tabela.HasKey("ProjetoId", "HabilidadeId"));

            modelBuilder.Entity<Projeto>().Property(p => p.OrcamentoMinimo).HasPrecision(12, 2);
            modelBuilder.Entity<Projeto>().Property(p => p.OrcamentoMaximo).HasPrecision(12, 2);
            modelBuilder.Entity<Projeto>()
                .Property(p => p.Status)
                .HasConversion(EmMaiusculas<StatusProjeto>())   // grava "ABERTO" / "ENCERRADO"
                .HasMaxLength(20);
            modelBuilder.Entity<Projeto>().HasIndex(p => new { p.Status, p.CategoriaId });
            modelBuilder.Entity<Projeto>()
                .Property(p => p.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // ---------- CANDIDATURA ----------

            // Projeto 1:N Candidaturas
            modelBuilder.Entity<Candidatura>()
                .HasOne(c => c.Projeto)
                .WithMany(p => p.Candidaturas)
                .HasForeignKey(c => c.ProjetoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Freelancer 1:N Candidaturas
            modelBuilder.Entity<Candidatura>()
                .HasOne(c => c.Freelancer)
                .WithMany()
                .HasForeignKey(c => c.FreelancerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Um freelancer só pode ter UMA candidatura por vaga
            // (no banco: CONSTRAINT UQ_Projeto_Freelancer UNIQUE (ProjetoId, FreelancerId)).
            // O controller verifica antes, mas o banco é a última barreira
            // (protege até contra dois cliques rápidos no botão "Enviar").
            modelBuilder.Entity<Candidatura>()
                .HasIndex(c => new { c.ProjetoId, c.FreelancerId })
                .IsUnique()
                .HasDatabaseName("UQ_Projeto_Freelancer");

            modelBuilder.Entity<Candidatura>().Property(c => c.ValorProposto).HasPrecision(12, 2);
            modelBuilder.Entity<Candidatura>()
                .Property(c => c.Status)
                .HasConversion(EmMaiusculas<StatusCandidatura>())   // grava "PENDENTE", "ACEITA"...
                .HasMaxLength(20);
            modelBuilder.Entity<Candidatura>()
                .Property(c => c.DataCandidatura)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // ---------- INTERESSE ----------

            modelBuilder.Entity<Interesse>()
                .HasOne(i => i.Projeto)
                .WithMany()
                .HasForeignKey(i => i.ProjetoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Interesse>()
                .HasOne(i => i.Freelancer)
                .WithMany()
                .HasForeignKey(i => i.FreelancerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cada lado decide UMA vez sobre o par (vaga, freelancer)
            modelBuilder.Entity<Interesse>()
                .HasIndex(i => new { i.ProjetoId, i.FreelancerId, i.Origem })
                .IsUnique();

            // "FREELANCER"/"CLIENTE" e "INTERESSE"/"REJEICAO"
            modelBuilder.Entity<Interesse>().Property(i => i.Origem)
                .HasConversion(EmMaiusculas<OrigemInteresse>()).HasMaxLength(20);
            modelBuilder.Entity<Interesse>().Property(i => i.Tipo)
                .HasConversion(EmMaiusculas<TipoInteresse>()).HasMaxLength(20);
            modelBuilder.Entity<Interesse>()
                .Property(i => i.DataRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // ---------- MATCH ----------

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Projeto)
                .WithMany()
                .HasForeignKey(m => m.ProjetoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Freelancer)
                .WithMany()
                .HasForeignKey(m => m.FreelancerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Candidatura 1:1 Match
            modelBuilder.Entity<Match>()
                .HasOne(m => m.Candidatura)
                .WithOne()
                .HasForeignKey<Match>(m => m.CandidaturaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Um único match por par (vaga, freelancer)
            modelBuilder.Entity<Match>()
                .HasIndex(m => new { m.ProjetoId, m.FreelancerId })
                .IsUnique();
        }

        // Converte enum <-> texto em MAIÚSCULAS (padrão do banco do grupo).
        // Ex.: StatusCandidatura.Pendente <-> "PENDENTE"
        private static ValueConverter<TEnum, string> EmMaiusculas<TEnum>() where TEnum : struct, Enum
        {
            return new ValueConverter<TEnum, string>(
                valor => valor.ToString().ToUpperInvariant(),
                texto => Enum.Parse<TEnum>(texto, true));
        }
    }
}