﻿using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace CBHK.Domain.Model
{
    public class EnvironmentConfig
    {
        public string Visibility { get; set; }
        public string CloseToTray { get; set; }
        public string ShowNotice { get; set; }
    }
}