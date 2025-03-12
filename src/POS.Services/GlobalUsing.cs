global using System.Text.Json;
global using StackExchange.Redis;
global using POS.Core.Services.Contract;
global using Microsoft.Extensions.Caching.Distributed;
global using Microsoft.Extensions.Caching.StackExchangeRedis;

global using POS.Core.Entities.Company;
global using POS.Core.Services.Contract.CompanyService;

global using Serilog;
global using POS.Core.Repository.Contract;


global using POS.Core.Services.Contract.CategoryServices;


global using POS.Core.Entities.Item;
global using POS.Core.Services.Contract.ItemServices;

global using POS.Core.Specifications;

global using POS.Core.Specifications.MenuSalesItemsSpecs;
global using POS.Core.Services.Contract.UserSettingServices;
global using POS.Core.Entities.UserSettings;


global using Microsoft.AspNetCore.Identity;
global using Microsoft.Extensions.Configuration;
global using Microsoft.IdentityModel.Tokens;
global using POS.Core.Services.Contract.AccountDomainContracts;
global using POS.Repository.Data;
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global using System.Text;



global using POS.Core.Entities.DineIn;
global using POS.Core.Services.Contract.DineInServices;
global using POS.Core.Services.Contract.AppDateServices;
global using POS.Core.Entities.Date;