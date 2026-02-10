using AutoMapper;
using POS.Core.Entities;
using POS.Core.Entities.DineIn;
using POS.Core.Entities.OrderEntity;
using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.KitchenDtos;
using BlazorBase.Models;

namespace POS.API.Helpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CreateCompanyDto, Company>()
            .ForMember(c => c.NormalizedEnglishName,
            c =>
            c.MapFrom(c => c!.EnglishName!.ToUpper()))
           .ForMember(c => c.NormalizedEmail,
           c =>
           c.MapFrom(c => c!.Email!.ToUpper()));

        CreateMap<UpdatedCompanyDto, Company>()
           .ForMember(c => c.NormalizedEnglishName,
            c =>
            c.MapFrom(c => c!.EnglishName!.ToUpper()))
           .ForMember(c => c.NormalizedEmail,
           c =>
           c.MapFrom(c => c!.Email!.ToUpper()));


        CreateMap<BranchDto, Branch>()
           .ForMember(b => b.NormalizedName,
           b =>
           b.MapFrom(b => b!.Name!.ToUpper()))
           .ForMember(b => b.ImagePath, b => b.Ignore());

        CreateMap<UpdatedBranchDto, Branch>()
           .ForMember(b => b.NormalizedName,
           b =>
           b.MapFrom(b => b!.Name!.ToUpper()))
           .ForMember(b => b.ImagePath, b => b.Ignore());

        CreateMap<Branch, BranchToReturnDto>()
            .ForMember(b => b.Logo, b =>
            b!.MapFrom<ImageUrlResolver<Branch, BranchToReturnDto>>());

        CreateMap<CategoryDto, Category>()
            .ForMember(c => c.Id, c => c.Ignore())
            .ForMember(c => c.NormalizedEnglishName, c =>
            c.MapFrom(c => c!.EnglishName!.ToUpper()));

        CreateMap<UpdatedCategoryDto, Category>()
            .ForMember(c => c.NormalizedEnglishName, c => c.MapFrom(c => c!.EnglishName!.ToUpper()));


        CreateMap<CreateAttributeDto, Attributes>()
            .ForMember(a => a.Id, a => a.Ignore());

        CreateMap<AttributeItem, AttributeItemToReturnDto>();
        CreateMap<Category, CategoryToReturnDto>();

        CreateMap<AttributeItemDto, AttributeItem>();

        CreateMap<MenuSalesItemsDto, MenuSalesItems>()
            .ForMember(s => s.ImagePath, s => s.Ignore())
            .ForMember(s => s.NormalizedEnglishName, s =>
            s.MapFrom(s => s!.EnglishName!.ToUpper()))
            .ForMember(i => i.AttributeId, i =>
            i.MapFrom(i => i.AttributeId));





        CreateMap<UpdatedItemDto, MenuSalesItems>()
            .ForMember(s => s.NormalizedEnglishName, s =>
            s.MapFrom(s => s!.EnglishName!.ToUpper()));


        CreateMap<Attributes, AttributeToReturnDto>()
            .ForMember(dest => dest.AttributeItems, opt =>
                opt.MapFrom(src => src.AttributeItems));

        CreateMap<AttributeItem, AttributeItemToReturnDto>();


        CreateMap<UpdatedAttributeDto, Attributes>()
            .ForMember(dest => dest.AttributeItems, opt =>
                opt.MapFrom(src => src.AttributeItems));

        CreateMap<MenuSalesItems, MenuSalesItemsToReturnDto>()
    .ForMember(s => s.ImageUrl,
        s => s.MapFrom<ImageUrlResolver<MenuSalesItems, MenuSalesItemsToReturnDto>>())
    .ForMember(dest => dest.PrintInBackupReceiptFromItem,
    src =>
    src.MapFrom(s => s.PrintInBackupReceipt))
    .ForMember(dest => dest.PrintInBackupReceiptFromCategory,
    src =>
    src.MapFrom(s => s.Category!.PrintInBackupReceipt))
    .ForMember(dest => dest.CategoryId,
        c => c.MapFrom(c => c.CategoryId))
    .ForMember(dest => dest.ItemKitchenTypeId,
        src => src.MapFrom(i => i.KitchenTypeId))
    .ForMember(dest => dest.CategoryKitchenTypeId,
        src => src.MapFrom(i => i.Category != null ? i.Category.KitchenTypeId : null))
    .ForMember(dest => dest.Attributes,
        opt => opt.MapFrom(src =>
            src.HasAttribute && src.Attribute != null
                ? src.Attribute.AttributeItems.Select(ai => new MenuSalesItemAttributes
                {
                    AppearanceIndex = ai.AppearanceIndex,
                    GroupItems = new List<MenuSalesItemsGroupDto>
                    {
                        new MenuSalesItemsGroupDto
                        {
                            Id = ai.Id,
                            ArabicName = ai.RelatedMenuItem!.ArabicName,
                            EnglishName = ai.RelatedMenuItem.EnglishName,
                            Price = ai.RelatedMenuItem.Price,
                            AttributePrice = ai.RelatedMenuItem.AttributePrice
                        }
                    }
                }).ToList()
                : null
        ));


        CreateMap<AppUser, UserDto>()
            .ForMember(dest => dest.UserId, src => src.MapFrom(s => s.Id));

        CreateMap<TableDto, Table>();
        CreateMap<TableGroupDto, TableGroup>();
        CreateMap<TableGroupToReturnDto, TableGroup>().ReverseMap();
        CreateMap<Table, TableToReturnDto>()
            .ForMember(x => x.TableState, src => src.MapFrom(s => s.TableState.ToString()))
            .ReverseMap();


        CreateMap<AppUser, CaptainOrderUserToReturnDto>();

        CreateMap<AppDate, AppDateToReturnDto>();

        CreateMap<DeliveryZone, DeliveryZonesToReturnDto>().ReverseMap();
        CreateMap<DeliveryZoneDto, DeliveryZone>().ReverseMap();


        CreateMap<DeliveryCustomerInfo, DeliveryCustomerToReturnDto>()
        .ForMember(dest => dest.CustomerAddresses,
             opt =>
                opt.MapFrom(src => src.CustomerAddresses));

        CreateMap<CustomerAddress, CustomerAddressDto>();


        CreateMap<DeliveryCustomerDto, DeliveryCustomerInfo>()
            .ForMember(dest => dest.CustomerAddresses, opt => opt.MapFrom(src => new List<CustomerAddress>
            {
                new CustomerAddress
                {
                    BranchName = src.BranchName,
                    ZoneName = src.ZoneName,
                    HomeNumber = src.HomeNumber,
                    FloorNumber = src.FloorNumber,
                    FlatNumber = src.FlatNumber,
                    ClientAddress = src.ClientAddress,
                    AddressNote = src.AddressNote
                }
            }));

        CreateMap<DeliveryCustomerInfo, DeliveryCustomerDto>()
          .ForMember(dest => dest.ClientAddress,
          opt =>
          opt.MapFrom(src => src.CustomerAddresses));


        CreateMap<CustomerNewAddressDto, CustomerAddress>();


        CreateMap<KitchenType, KitchenTypeToReturnDto>()
               .ForMember(dest => dest.KitchenPrinter, opt
               => opt.MapFrom(src => src.KitchenPrinters))
               .ForMember(dest => dest.KitchenPrinterId, opt
               => opt.MapFrom(src => src.KitchenPrinterId));

        CreateMap<KitchenPrinters, KitchenPrintersDto>().ReverseMap();


        CreateMap<KitchenTypeDto, KitchenType>();
        CreateMap<KitchenPrinters, KitchenPrintersToReturnDto>();


        CreateMap<Orders, OrderDto>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.OrderID))
            .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.BranchID))
            .ForMember(dest => dest.TakeAwayCustomerName, opt => opt.MapFrom(src => src.TakeawayCustomerName))
            .ForMember(dest => dest.TakeawayCustomerPhone, opt => opt.MapFrom(src => src.TakeawayCustomerPhone))
            .ForMember(dest => dest.TotalOrderDiscount, opt => opt.MapFrom(src => src.TotalDiscount))
            .ForMember(dest => dest.Remaining, opt => opt.MapFrom(src => src.Remain))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Service))
            .ForMember(dest => dest.OrderType, opt => opt.MapFrom(src => src.OrderType.ToString()))
            .ForMember(dest => dest.OrderState, opt => opt.MapFrom(src => src.OrderState.ToString()))
            .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails)) // هنعمل مابنج تاني للجوه
            .ForMember(dest => dest.MachineName, opt => opt.MapFrom(src => src.MachineName))
            .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.OrderDate));

        CreateMap<OrderItemsDetails, TableItem>();

        // New mappings for DineInOrder and OrderTrack
        CreateMap<OrderItemsDetails, OrderItemsDetailsDto>()
            .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.ItemName ?? (src.MenuSalesItem != null ? src.MenuSalesItem.ArabicName : "")))
            .ForMember(dest => dest.ItemNameAr, opt => opt.MapFrom(src => src.ItemNameAr ?? (src.MenuSalesItem != null ? src.MenuSalesItem.ArabicName : "")))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId ?? (src.MenuSalesItem != null ? src.MenuSalesItem.CategoryId : null)))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName ?? (src.MenuSalesItem != null && src.MenuSalesItem.Category != null ? src.MenuSalesItem.Category.ArabicName : "")))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.UnitPrice ?? (src.MenuSalesItem != null ? src.MenuSalesItem.Price : 0)))
            .ForMember(dest => dest.OrderItemComments, opt => opt.MapFrom(src => src.OrderItemComments))
            // Map Kitchen Printing Properties
            .ForMember(dest => dest.ItemKitchenTypeId, opt => opt.MapFrom(src => src.MenuSalesItem != null ? src.MenuSalesItem.KitchenTypeId : null))
            .ForMember(dest => dest.CategoryKitchenTypeId, opt => opt.MapFrom(src => src.MenuSalesItem != null && src.MenuSalesItem.Category != null ? src.MenuSalesItem.Category.KitchenTypeId : null))
            .ForMember(dest => dest.PrintInBackupReceiptFromItem, opt => opt.MapFrom(src => src.MenuSalesItem != null ? src.MenuSalesItem.PrintInBackupReceipt : null))
            .ForMember(dest => dest.PrintInBackupReceiptFromCategory, opt => opt.MapFrom(src => src.MenuSalesItem != null && src.MenuSalesItem.Category != null ? src.MenuSalesItem.Category.PrintInBackupReceipt : null))
            .ReverseMap()
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.OrderItemComments, opt => opt.MapFrom(src => src.OrderItemComments));

        CreateMap<OrderItemComment, OrderItemCommentDto>().ReverseMap();
        CreateMap<OrderItemAttributes, OrderItemAttributesDto>().ReverseMap();
        CreateMap<OrderTrack, OrderTrackDto>().ReverseMap();

        CreateMap<Orders, DineInOrderDto>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.OrderID))
            .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.BranchID))
            .ForMember(dest => dest.OrderDateTime, opt => opt.MapFrom(src => src.OrderDate))
            .ForMember(dest => dest.OrderState, opt => opt.MapFrom(src => src.OrderState.HasValue ? src.OrderState.Value.ToString() : "Open"))
            .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.Discount))
            .ForMember(dest => dest.TotalDiscount, opt => opt.MapFrom(src => src.TotalDiscount))
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
            .ForMember(dest => dest.GrandTotal, opt => opt.MapFrom(src => src.GrandTotal))
            .ForMember(dest => dest.Tax, opt => opt.MapFrom(src => src.Tax))
            .ForMember(dest => dest.Service, opt => opt.MapFrom(src => src.Service))
            .ForMember(dest => dest.TableId, opt => opt.MapFrom(src => src.TableID))
            .ForMember(dest => dest.TableName, opt => opt.MapFrom(src => src.TableName))
            .ForMember(dest => dest.CaptainName, opt => opt.MapFrom(src => src.WaiterName))
            .ForMember(dest => dest.CaptainId, opt => opt.MapFrom(src => src.WaiterID.HasValue ? src.WaiterID.Value.ToString() : null))
            .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails))
            .ForMember(dest => dest.MachineName, opt => opt.MapFrom(src => src.MachineName))
            .ForMember(dest => dest.CustomerCount, opt => opt.MapFrom(src => src.CustomerCount))
            .ForMember(dest => dest.MaleCount, opt => opt.MapFrom(src => src.MaleCount))
            .ForMember(dest => dest.FemaleCount, opt => opt.MapFrom(src => src.FemaleCount))
            .ForMember(dest => dest.DiscountedItems, opt => opt.MapFrom(src => src.DiscountedItems))
            .ForMember(dest => dest.ReservationCustomerName, opt => opt.MapFrom(src => src.ReservationCustomerName))
            .ForMember(dest => dest.ReservationCustomerPhone, opt => opt.MapFrom(src => src.ReservationCustomerPhone))
            .ForMember(dest => dest.ReservationPaid, opt => opt.MapFrom(src => src.ReservationPaid))
            .ForMember(dest => dest.ScheduleDateTime, opt => opt.MapFrom(src => src.ScheduleDateTime))
            .ForMember(dest => dest.MaleCount, opt => opt.MapFrom(src => src.MaleCount))
            .ForMember(dest => dest.FemaleCount, opt => opt.MapFrom(src => src.FemaleCount))
            .ReverseMap()
            .ForMember(dest => dest.OrderID, opt => opt.MapFrom(src => src.OrderId))
            .ForMember(dest => dest.BranchID, opt => opt.MapFrom(src => src.BranchId))
            .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.OrderDateTime))
            .ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.DiscountAmount))
            .ForMember(dest => dest.TotalDiscount, opt => opt.MapFrom(src => src.TotalDiscount))
            .ForMember(dest => dest.DiscountedItems, opt => opt.MapFrom(src => src.DiscountedItems))
            .ForMember(dest => dest.TableID, opt => opt.MapFrom(src => src.TableId))
            .ForMember(dest => dest.TableName, opt => opt.MapFrom(src => src.TableName))
            .ForMember(dest => dest.WaiterName, opt => opt.MapFrom(src => src.CaptainName))
            .ForMember(dest => dest.OrderType, opt => opt.MapFrom(src => OrderTypes.DineIn))
            .ForMember(dest => dest.ReservationCustomerName, opt => opt.MapFrom(src => src.ReservationCustomerName))
            .ForMember(dest => dest.ReservationCustomerPhone, opt => opt.MapFrom(src => src.ReservationCustomerPhone))
            .ForMember(dest => dest.ReservationPaid, opt => opt.MapFrom(src => src.ReservationPaid))
            .ForMember(dest => dest.ScheduleDateTime, opt => opt.MapFrom(src => src.ScheduleDateTime))
            .ForMember(dest => dest.MaleCount, opt => opt.MapFrom(src => src.MaleCount))
            .ForMember(dest => dest.FemaleCount, opt => opt.MapFrom(src => src.FemaleCount))
            .ForMember(dest => dest.OrderState, opt => opt.MapFrom(src => src.OrderState == "Open" ? OrderStates.Pending : 
                                                                  src.OrderState == "Reserved" ? OrderStates.Reserved : OrderStates.Completed));

    }
}
