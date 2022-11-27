using CliWrap;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppListCmd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SiteListController : ControllerBase
    {
        IConfiguration Config;
        public SiteListController(IConfiguration configuration)
        {
            Config = configuration;
        }

        public class SiteResult
        {
            public string SType { get; set; }
            public string Name { get; set; }
            public string SiteId { get; set; }
            public string[] BindList { get; set; }
            public string State { get; set; }
        }

        [HttpGet]
        public async Task<List<SiteResult>> Get()
        {
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            // ⚠ This particular example can also be simplified with ExecuteBufferedAsync().
            // Continue reading below!
            var result = await Cli.Wrap("AppCmd.exe")
                .WithArguments(Config["Args"])
                .WithWorkingDirectory(@"C:\Windows\System32\inetsrv\")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .ExecuteAsync();

            
            var stdOut = stdOutBuffer.ToString();
            var lines = stdOut.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            
            //SITE "QRCodeServer" (id:53,bindings:http/192.168.2.72:6767:,http/*:6767:,state:Started)
            Regex regexObj = new Regex(@"(\w+) ""([\w \.-]+)"" \(id\:(\d+),bindings:([\w\/\:\.\*\,]+),state\:(\w+)\)");
            var serverIp = Config["Ip"];
            var siteResults = new List<SiteResult>();
            var excludeList = Config["AppExcludeList"].Split(',');

            foreach (var line in lines)
            {
                var res = regexObj.Match(line);
                var bnd = res.Groups[4].Value.Replace("http/", "http://").Replace("*", serverIp);
                var bndList = bnd.Split(',').Distinct().Select(x => x.TrimEnd(':')).ToArray();

                if (res.Success)
                {
                    if (excludeList.Contains(res.Groups[2].Value))
                        continue;

                    var item = new SiteResult()
                    {
                        SType = res.Groups[1].Value,
                        Name = res.Groups[2].Value,
                        SiteId = res.Groups[3].Value,
                        BindList = bndList,
                        State = res.Groups[5].Value
                    };
                    siteResults.Add(item);
                }
            }

            return  siteResults;
        }
    }
}
