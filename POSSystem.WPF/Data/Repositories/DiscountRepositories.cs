

namespace POSSystem.WPF.Data.Repositories
{
    public class DiscountRepositories : Repository<Discount>, IDiscountRepositories
    {
        public DiscountRepositories(POSDbContext context) : base(context)
        {
            
        }




    }

}
