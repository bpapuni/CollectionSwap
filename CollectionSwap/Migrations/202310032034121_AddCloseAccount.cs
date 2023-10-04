namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCloseAccount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "ClosedAccount", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "ClosedAccount");
        }
    }
}
