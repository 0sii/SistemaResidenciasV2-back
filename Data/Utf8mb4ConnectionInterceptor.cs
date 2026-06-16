using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WebApiVinculacionProyectosV2.Data
{
    public sealed class Utf8mb4ConnectionInterceptor : DbConnectionInterceptor
    {
        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SET NAMES utf8mb4 COLLATE utf8mb4_spanish_ci;";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}