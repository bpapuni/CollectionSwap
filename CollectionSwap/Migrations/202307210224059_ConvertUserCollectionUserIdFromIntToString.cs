namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ConvertUserCollectionUserIdFromIntToString : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.UserCollections", "UserId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.UserCollections", "UserId", c => c.Int(nullable: false));
        }
    }
}
