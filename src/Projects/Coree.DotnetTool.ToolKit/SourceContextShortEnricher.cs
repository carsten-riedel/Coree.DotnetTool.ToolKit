using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog.Core;
using Serilog.Events;

namespace Coree.DotnetTool.ToolKit
{
    public class SourceContextShortEnricher : ILogEventEnricher
    {
        private readonly List<string> suffixesToRemove = new List<string>() { "Service", "Services" , "AsyncCommand", "Command"  };
        private readonly bool cutTTypes;
        private readonly bool removeDots;
        private readonly bool removeSuffixes;
        private readonly int padding;

        public SourceContextShortEnricher(bool cutTTypes = true, bool removeDots = true, bool removeSuffixes = true,int padding = 15)
        {
            this.cutTTypes = cutTTypes;
            this.removeDots = removeDots;
            this.removeSuffixes = removeSuffixes;
            this.padding = padding; 
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue value))
            {
                if (value is ScalarValue scalarValue && scalarValue.Value is string sourceContext)
                {
                    var shortSourceContext = sourceContext;

                    if (cutTTypes)
                    {
                        var TType = shortSourceContext.IndexOf("`1");
                        if (TType > -1)
                        {
                            shortSourceContext = shortSourceContext.Substring(0, TType);
                        }
                    }

                    if (removeDots)
                    {
                        var shortSourceContexts = shortSourceContext.Split('.');
                        shortSourceContext = shortSourceContexts[shortSourceContexts.Length - 1];
                    }

                    if (removeSuffixes)
                    {
                        // Check and remove any of the specified suffixes
                        foreach (var suffix in suffixesToRemove)
                        {
                            if (shortSourceContext.EndsWith(suffix))
                            {
                                shortSourceContext = shortSourceContext.Substring(0, shortSourceContext.Length - suffix.Length);
                                break; // Assuming you only want to remove the first matching suffix
                            }
                        }
                    }

                    shortSourceContext = shortSourceContext.PadRight(padding);
                    // Create a new property with the modified value and add/update it in the log event.
                    var modifiedProperty = propertyFactory.CreateProperty("SourceContextShort", shortSourceContext);
                    logEvent.AddOrUpdateProperty(modifiedProperty);
                }
            }
        }
    }
}
