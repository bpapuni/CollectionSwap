namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddressLine2Nullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Addresses", "LineTwo", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Addresses", "LineTwo", c => c.String(nullable: false));
        }
    }
}
