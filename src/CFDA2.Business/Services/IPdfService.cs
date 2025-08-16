using System.IO;
using System.Threading.Tasks;

namespace Portal.Server.Services;

public interface IPdfService
{
    Task<byte[]> GenerateStatementAsync(int familyId, int year, int month);
}