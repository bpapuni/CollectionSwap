namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBoolToDisplaySwapInSwapHistory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Swaps", "SenderDisplaySwap", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.Swaps", "ReceiverDisplaySwap", c => c.Boolean(nullable: false, defaultValue: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Swaps", "ReceiverDisplaySwap");
            DropColumn("dbo.Swaps", "SenderDisplaySwap");
        }
    }
}
