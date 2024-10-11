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
global using POS.Repository.Data;
global using StackExchange.Redis;
global using POS.Repository.Identity;
global using Microsoft.AspNetCore.Mvc;
global using POS.API.Dtos.CompanyDtos;
global using POS.Core.Services.Contract;
global using POS.Core.Entities.Identity;
global using POS.Repository.Data.DataSeed;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.IdentityModel.Tokens;
global using Microsoft.AspNetCore.Authentication.JwtBearer;

global using POS.Core.Services.Contract.CompanyService;
global using POS.Services.ItemServices;
global using POS.Core.Specifications.AttributeSpecs;
global using POS.API.Dtos.AttributeDtos;






global using POS.API.Dtos.ItemDto;
global using POS.Core.Entities.Item;
global using POS.Core.Services.Contract.ItemServices;



global using POS.Repository;
global using POS.Core.Repository.Contract;
global using POS.Repository.Repositories;
global using POS.Services.CompanyService;

global using POS.Core.Entities.Company;


global using System.ComponentModel.DataAnnotations;

global using POS.API.Dtos.CategoryDtos;
global using POS.Core.Entities.Category;

global using POS.Core.Services.Contract.CategoryServices;

global using POS.Services.CategoryService;

