using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunctionTempFolderFreeSpace
{
    public class GetTempFolderAvailableSpace
    {
        private readonly ILogger _logger;

        public GetTempFolderAvailableSpace(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetTempFolderAvailableSpace>();
        }

        [Function("GetTempFolderAvaiableSpace")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            string tempFolder = Path.GetTempPath();
            long managedAPIResult = GetByDotnetAPI(tempFolder);
            ulong nativeAPIResult = GetByPInvoke(tempFolder);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString($"Get temp folder available space by managed API: {managedAPIResult} and by native API: {nativeAPIResult}.");

            string homeFolderVariable = @"%HOME%";
            string homeFolder = Environment.ExpandEnvironmentVariables(homeFolderVariable);
            if (string.Equals(homeFolderVariable, homeFolder))
            {
                response.WriteString("No home env var.");
            }
            else
            {
                response.WriteString(Environment.NewLine + $"Get %HOME% folder available space by managed API: {GetByDotnetAPI(homeFolder)} and by native API: {GetByPInvoke(homeFolder)}.");
            }

            return response;
        }

        private long GetByDotnetAPI(string tempFolder)
        {
            var driveInfo = new DriveInfo(tempFolder);
            return driveInfo.AvailableFreeSpace;
        }

        private ulong GetByPInvoke(string tempFolder)
        {
            // Making sure there's tailing slash.
            // Refer to https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getdiskfreespaceexa
            tempFolder = tempFolder.TrimEnd('\\') + '\\';

            return GetDiskFreeSpace(tempFolder);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        private static ulong GetDiskFreeSpace(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (!GetDiskFreeSpaceEx(path, out ulong freeSpace, out _, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return freeSpace;
        }
    }
}
