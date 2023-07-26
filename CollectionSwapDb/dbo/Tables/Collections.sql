CREATE TABLE [dbo].[Collections] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (MAX) NOT NULL,
    [ItemListJSON] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.CardSets] PRIMARY KEY CLUSTERED ([Id] ASC)
);