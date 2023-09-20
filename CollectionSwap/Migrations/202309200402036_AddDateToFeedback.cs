namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDateToFeedback : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Feedbacks", "DatePlaced", c => c.DateTimeOffset(nullable: false, precision: 7));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Feedbacks", "DatePlaced");
        }
    }
}
