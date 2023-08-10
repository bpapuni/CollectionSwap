namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EditAddressModel : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Addresses", "UserId", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Addresses", "UserId", c => c.String(nullable: false));
        }
    }
}
