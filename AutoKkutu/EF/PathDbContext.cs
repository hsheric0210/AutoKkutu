using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoKkutu.EF
{
	public sealed class PathDbContext : DbContext
	{
		private readonly DatabaseProvider ProviderType;
		private readonly string ConnectionString;

		public DbSet<Word> Word
		{
			get; set;
		}

		public DbSet<SingleWordIndex> AttackWordIndex
		{
			get; set;
		}
		public DbSet<SingleWordIndex> EndWordIndex
		{
			get; set;
		}

		public DbSet<DoubleWordIndex> KkutuAttackWordIndex
		{
			get; set;
		}
		public DbSet<DoubleWordIndex> KkutuEndWordIndex
		{
			get; set;
		}

		public PathDbContext(DatabaseProvider providerType, string connectionString)
		{
			ProviderType = providerType;
			ConnectionString = connectionString;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			switch (ProviderType)
			{
				case DatabaseProvider.Sqlite:
					optionsBuilder.UseSqlite(ConnectionString);
					break;
				case DatabaseProvider.PostgreSql:
					optionsBuilder.UseNpgsql(ConnectionString);
					break;
				case DatabaseProvider.MySql:
					optionsBuilder.UseMySQL(ConnectionString);
					break;
			}
		}
	}
}
