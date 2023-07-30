namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ConvertSwapDateTimeColumnsToDateTime2 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Swaps", "StartDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Swaps", "EndDate", c => c.DateTimeOffset(nullable: false, precision: 7));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Swaps", "EndDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Swaps", "StartDate", c => c.DateTime(nullable: false));
        }
    }
}
