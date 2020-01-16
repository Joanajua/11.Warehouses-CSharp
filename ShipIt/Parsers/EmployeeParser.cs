﻿using System.Collections.Generic;
using System.Linq;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;

namespace ShipIt.Parsers
{
    public class EmployeeRequestModel
    {
        public string Gtin { get; set; }
        public string CompanyName { get; set; }
        public string EmployeeName { get; set; }
        public string Weight { get; set; }
        public string LowerThreshold { get; set; }
        public string Discontinued { get; set; }
        public string MinimumOrderQuantity { get; set; }
    }
}