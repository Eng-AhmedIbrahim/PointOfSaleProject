global using Serilog;
global using System.Net;
global using AutoMapper;
global using System.Text;
global using POS.Services;
global using POS.API.Errors;
global using POS.API.Helpers;
global using System.Text.Json;
global using POS.API.Extensions;
global using POS.API.MiddleWare;
global using StackExchange.Redis;
global using Microsoft.AspNetCore.Mvc;
global using POS.Contract.Dtos.ItemDto;
global using POS.Core.Services.Contract;
global using POS.Core.Entities.Identity;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.IdentityModel.Tokens;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using POS.Core.Services.Contract.CompanyService;
global using POS.Services.ItemServices;
global using POS.Core.Specifications.AttributeSpecs;
global using POS.Contract.Dtos.AttributeDtos;






global using POS.Contract.Dtos.CategoryDtos;
global using POS.Core.Entities.Item;
global using POS.Core.Services.Contract.ItemServices;



global using POS.Repository;
global using POS.Core.Repository.Contract;
global using POS.Services.CompanyService;

global using POS.Core.Entities.Company;


global using System.ComponentModel.DataAnnotations;

global using POS.Contract.Dtos.CompanyDtos;

global using POS.Core.Services.Contract.CategoryServices;

global using POS.Services.CategoryService;

global using POS.Core.Entities.UserSettings;
global using POS.Contract.Dtos.AccountDtos;
global using POS.Core.Services.Contract.AccountDomainContracts;
global using System.Security.Claims;
global using POS.Core.Services.Contract.UserSettingServices;
global using POS.Services.UserSettingsService;
global using QuestPDF.Infrastructure;




global using POS.Contract.Dtos.TakeawayOrderDtos;
global using POS.Core.Entities.OrderEntity;
global using POS.Core.Services.Contract.OrderServices;

global using Pos.Repository.Repositories;
global using POS.Services.AuthModuleService;


global using POS.Core.Services.Contract.DineInServices;
global using POS.Services.DineInService;

global using POS.Contract.Dtos.DineInDtos;
global using POS.Core.Entities.DineIn;
global using POS.Core.Entities.Kitchen;
global using Pos.Repository.Identity;
global using POS.Core.Services.Contract.AppDateServices;
global using Pos.Repository.Data;
global using Pos.Repository.Data.DataSeed;
global using POS.Contract.Dtos.AppDateDtos;
global using POS.Core.Entities.Categorie;
global using POS.Core.Entities.Date;
global using POS.Services.AppDateServices;
global using POS.Services.OrderServices;