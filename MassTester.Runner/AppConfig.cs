using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AppConfig
{
    public AppConfig()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        var available = new List<FileInfo>();

        foreach (var config in dir.EnumerateFiles("*.config", SearchOption.TopDirectoryOnly))
        {
            available.Add(config);
        }

        var existing = new FileInfo((string)AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE"));

        if (existing.Exists)
        {
            return;
        }

        if (available.Count > 1)
        {
            throw new System.Configuration.ConfigurationErrorsException("The application didn't have a matching config-file and multiple alternatives were found. Please make sure that only one config is available in the executing application's directory.");
        }

        if (available.Count == 0)
        {
            throw new IOException("No app.config could be found.");
        }

        var selection = available.First();
        AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", selection.FullName);
    }

    public string GetOutputFormat()
    {
        try
        {
            return System.Configuration.ConfigurationManager.AppSettings["Runner:OutputFormat"];
        }
        catch (Exception)
        {
            return "nunit";
        }
    }
}