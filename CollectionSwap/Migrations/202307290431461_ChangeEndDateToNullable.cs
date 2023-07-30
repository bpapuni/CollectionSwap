namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeEndDateToNullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Swaps", "EndDate", c => c.DateTimeOffset(precision: 7));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Swaps", "EndDate", c => c.DateTimeOffset(nullable: false, precision: 7));
        }
    }
}
