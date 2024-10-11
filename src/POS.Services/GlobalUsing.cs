global using System;
global using System.Text.Json;
global using StackExchange.Redis;
global using POS.Core.Services.Contract;
global using Microsoft.Extensions.Caching.Distributed;
global using Microsoft.Extensions.Caching.StackExchangeRedis;

global using POS.Core.Entities.Company;
global using POS.Core.Services.Contract.CompanyService;

global using Serilog;
global using POS.Core.Repository.Contract;


global using POS.Core.Entities.Category;
global using POS.Core.Services.Contract.CategoryServices;


global using POS.Core.Entities.Item;
global using POS.Core.Services.Contract.ItemServices;

global using POS.Core.Specifications;

global using POS.Core.Specifications.MenuSalesItemsSpecs;