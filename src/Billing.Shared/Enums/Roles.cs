namespace Billing.Shared.Enums;

/// <summary>
/// Application-level user roles. Stored in the database and used for authorization.
/// </summary>
public static class Roles
{
    public const string GlobalAdmin = "GlobalAdmin";
    public const string ShopAdmin = "ShopAdmin";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string Staff = "Staff";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        GlobalAdmin, ShopAdmin, Manager, Cashier, Staff
    };
}

/// <summary>
/// Permission constants used by the permission-based authorization handler.
/// </summary>
public static class Permissions
{
    public const string ProductsView = "Products.View";
    public const string ProductsCreate = "Products.Create";
    public const string ProductsEdit = "Products.Edit";
    public const string ProductsDelete = "Products.Delete";

    public const string SalesCreate = "Sales.Create";
    public const string SalesCancel = "Sales.Cancel";

    public const string CustomersView = "Customers.View";
    public const string CustomersCreate = "Customers.Create";
    public const string CustomersEdit = "Customers.Edit";
    public const string CustomersDelete = "Customers.Delete";

    public const string PurchasesView = "Purchases.View";
    public const string PurchasesCreate = "Purchases.Create";

    public const string InventoryView = "Inventory.View";
    public const string InventoryAdjust = "Inventory.Adjust";

    public const string ReportsView = "Reports.View";
    public const string ExpensesView = "Expenses.View";
    public const string ExpensesManage = "Expenses.Manage";

    public const string SettingsManage = "Settings.Manage";
    public const string StaffManage = "Staff.Manage";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        ProductsView, ProductsCreate, ProductsEdit, ProductsDelete,
        SalesCreate, SalesCancel,
        CustomersView, CustomersCreate, CustomersEdit, CustomersDelete,
        PurchasesView, PurchasesCreate,
        InventoryView, InventoryAdjust,
        ReportsView, ExpensesView, ExpensesManage,
        SettingsManage, StaffManage
    };
}
