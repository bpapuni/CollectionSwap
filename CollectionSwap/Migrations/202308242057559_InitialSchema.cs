namespace CollectionSwap.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialSchema : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Addresses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(),
                        FullName = c.String(nullable: false),
                        CompanyName = c.String(),
                        LineOne = c.String(nullable: false),
                        LineTwo = c.String(),
                        PostCode = c.String(nullable: false),
                        City = c.String(nullable: false),
                        Created = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Collections",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        ItemListJSON = c.String(),
                    })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.HeldItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ItemListJSON = c.String(),
                        Swap_Id = c.Int(),
                        UserCollection_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Swaps", t => t.Swap_Id)
                .ForeignKey("dbo.UserCollections", t => t.UserCollection_Id)
                .Index(t => t.Swap_Id)
                .Index(t => t.UserCollection_Id);
            
            CreateTable(
                "dbo.Swaps",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CollectionId = c.Int(nullable: false),
                        SenderUserCollectionId = c.Int(nullable: false),
                        ReceiverUserCollectionId = c.Int(nullable: false),
                        SenderId = c.String(nullable: false, maxLength: 128),
                        ReceiverId = c.String(nullable: false, maxLength: 128),
                        SenderItemIdsJSON = c.String(nullable: false),
                        ReceiverItemIdsJSON = c.String(nullable: false),
                        Status = c.String(nullable: false),
                        SenderConfirmSent = c.Boolean(nullable: false),
                        ReceiverConfirmSent = c.Boolean(nullable: false),
                        SenderConfirmReceieved = c.Boolean(nullable: false),
                        ReceiverConfirmReceieved = c.Boolean(nullable: false),
                        StartDate = c.DateTimeOffset(nullable: false, precision: 7),
                        EndDate = c.DateTimeOffset(precision: 7),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Collections", t => t.CollectionId, cascadeDelete: false)
                .ForeignKey("dbo.AspNetUsers", t => t.ReceiverId, cascadeDelete: false)
                .ForeignKey("dbo.AspNetUsers", t => t.SenderId, cascadeDelete: false)
                .Index(t => t.CollectionId)
                .Index(t => t.SenderId)
                .Index(t => t.ReceiverId);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");

            // Insert initial admin data
            //Sql($"INSERT INTO dbo.AspNetUsers (Id, Email, EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEndDateUtc, LockoutEnabled, AccessFailedCount, UserName) VALUES ('6b5afcbf-6f42-486e-bcb0-e7d80c230902', 'admin@swapper.co.nz', 1, 'AM0mrAh/WsskwdcMml71ZhgyJiOhKkAQqo/XjCwrsVizhSpneekETw35vAg35QXAUQ==', 'b1196dc8-b687-40db-bc45-c43f4078e851', NULL, 0, 0, NULL, 1, 0, 'SwapperAdmin')");

            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);

            //Sql("INSERT INTO dbo.AspNetRoles (Id, Name) VALUES ('7782a983-c0e4-4da8-8db7-1be9ed6a5fd5', 'Admin')");
            //Sql("INSERT INTO dbo.AspNetRoles (Id, Name) VALUES ('c5db9553-d0b2-4557-b32e-9dbcb4054b67', 'User')");

            CreateTable(
                "dbo.UserCollections",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        UserId = c.String(nullable: false, maxLength: 128),
                        CollectionId = c.Int(nullable: false),
                        ItemCountJSON = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                        Discriminator = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.HeldItems", "UserCollection_Id", "dbo.UserCollections");
            DropForeignKey("dbo.UserCollections", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.HeldItems", "Swap_Id", "dbo.Swaps");
            DropForeignKey("dbo.Swaps", "SenderId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Swaps", "ReceiverId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Swaps", "CollectionId", "dbo.Collections");
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.UserCollections", new[] { "UserId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.Swaps", new[] { "ReceiverId" });
            DropIndex("dbo.Swaps", new[] { "SenderId" });
            DropIndex("dbo.Swaps", new[] { "CollectionId" });
            DropIndex("dbo.HeldItems", new[] { "UserCollection_Id" });
            DropIndex("dbo.HeldItems", new[] { "Swap_Id" });
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.UserCollections");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.Swaps");
            DropTable("dbo.HeldItems");
            DropTable("dbo.Collections");
            DropTable("dbo.Addresses");
        }
    }
}
