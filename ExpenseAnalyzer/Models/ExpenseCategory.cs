namespace ExpenseAnalyzer.Models
{
    public enum ExpenseCategoryType
    {
        Transportation,
        Food,
        Healthcare,
        Housing,
        Children,
        Income,
        Expenses,
        Unknown
    }

    public enum ExpenseSubcategoryType
    {
        PublicTransportation,
        Fuel,
        Insurance,
        Maintenance,
        CarPurchase,
        Groceries,
        Resrtaurants,
        Takeout,
        Coffee,
        Humans,
        Pets,
        Shopping,
        Rent,
        Bills,
        Childcare,
        Education,
        Salary,
        Other,
        Reimbursement,
        Dividend,
        RSU,
        CreditPay,
        Transfer,
        Services,
        Subscription,
        Investment,
        Entertainment,
        Apperal,
        Travel
    }

    public class ExpenseCategory
    {
        public ExpenseCategoryType Category { get; set; }
        public ExpenseSubcategoryType Subcategory { get; set; }
        public bool Ignore { get; set; } = false;
    }
}
