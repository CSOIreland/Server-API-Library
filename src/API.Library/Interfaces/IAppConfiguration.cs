﻿using System.DirectoryServices.AccountManagement;

namespace API
{
    public interface IAppConfiguration
    {
        IDictionary<string, string> Settings { get; }
    }
}