CREATE TABLE [dbo].[UserCollections] (
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [UserId]        NVARCHAR (MAX) NOT NULL,
    [CollectionId]  INT            NOT NULL,
    [Name]          NVARCHAR (MAX) NOT NULL,
    [ItemCountJSON] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.UserCollections] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO
ALTER TABLE [dbo].[UserCollections]
    ADD DEFAULT ('') FOR [Name];