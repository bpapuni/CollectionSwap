namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdatedCollectionModel : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Collections", "Description", c => c.String(maxLength: 60));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Collections", "Description", c => c.String());
        }
    }
}
