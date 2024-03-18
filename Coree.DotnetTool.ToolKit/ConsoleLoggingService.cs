using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coree.DotnetTool.ToolKit
{
    public interface ILoggingService
    {
        Task LogAsync(string message);
    }

    public class ConsoleLoggingService : ILoggingService
    {
        public async Task LogAsync(string message)
        {
            await Task.Delay(500); // Simulate some async work
            Console.WriteLine(message);
        }
    }
}
