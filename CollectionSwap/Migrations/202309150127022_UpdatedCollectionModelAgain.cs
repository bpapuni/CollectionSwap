namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdatedCollectionModelAgain : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Collections", "Description", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Collections", "Description", c => c.String(maxLength: 60));
        }
    }
}
