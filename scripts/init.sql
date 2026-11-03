-- =========================================================
-- SCRIPT DE INICIALIZACIÓN AUTOMÁTICA (DOCKER)
-- =========================================================

-- 1. Crear la base de datos si no existe
IF DB_ID('PublisherDb') IS NULL
BEGIN
    CREATE DATABASE PublisherDb;
END
GO

USE PublisherDb;
GO

-- 2. Tabla ExcelFiles
IF OBJECT_ID('dbo.ExcelFiles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ExcelFiles](
        [Id] [uniqueidentifier] NOT NULL,
        [FileName] [nvarchar](255) NOT NULL,
        [FilePath] [nvarchar](500) NOT NULL,
        [Status] [tinyint] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [ProcessedAt] [datetime2](7) NULL,
        [Periodo] [date] NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- 3. Tabla AuditoriaFallidos
IF OBJECT_ID('dbo.AuditoriaFallidos', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditoriaFallidos](
        [Id] [uniqueidentifier] NOT NULL DEFAULT (newid()),
        [ExcelFileId] [uniqueidentifier] NOT NULL,
        [Fila] [int] NOT NULL,
        [CodigoProducto] [varchar](50) NULL,
        [Motivo] [varchar](500) NOT NULL,
        [FechaRegistro] [datetime] NOT NULL DEFAULT (getutcdate()),
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- 4. Tabla ProductRecords
IF OBJECT_ID('dbo.ProductRecords', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductRecords](
        [Id] [uniqueidentifier] NOT NULL,
        [ExcelFileId] [uniqueidentifier] NOT NULL,
        [Periodo] [date] NOT NULL,
        [CodigoProducto] [nvarchar](100) NOT NULL,
        [NombreProducto] [nvarchar](200) NOT NULL,
        [Precio] [decimal](18, 2) NOT NULL,
        [CreatedAt] [datetime2](7) NULL DEFAULT (getutcdate()),
        PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Foreign Key
    ALTER TABLE [dbo].[ProductRecords] WITH NOCHECK
    ADD CONSTRAINT [FK_ProductRecords_ExcelFiles] FOREIGN KEY([ExcelFileId])
    REFERENCES [dbo].[ExcelFiles] ([Id]);
END
GO

-- 5. SP: sp_CreateExcelFile
IF OBJECT_ID('dbo.sp_CreateExcelFile', 'P') IS NULL
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_CreateExcelFile]
        @Id UNIQUEIDENTIFIER,
        @FileName NVARCHAR(255),
        @FilePath NVARCHAR(500),
        @Status TINYINT,
        @CreatedAt DATETIME2
    AS
    BEGIN
        SET NOCOUNT ON;
        INSERT INTO ExcelFiles (Id, FileName, FilePath, Status, CreatedAt, ProcessedAt)
        VALUES (@Id, @FileName, @FilePath, @Status, @CreatedAt, NULL);

        SELECT @Id as Id;
    END;
    ')
END
GO

-- 6. SP: sp_GetExcelFileById
IF OBJECT_ID('dbo.sp_GetExcelFileById', 'P') IS NULL
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_GetExcelFileById]
        @Id UNIQUEIDENTIFIER
    AS
    BEGIN
        SET NOCOUNT ON;
        SELECT Id, FileName, FilePath, Status, CreatedAt, ProcessedAt
        FROM ExcelFiles
        WHERE Id = @Id;
    END;
    ')
END
GO

-- 7. SP: sp_UpdateExcelFile
IF OBJECT_ID('dbo.sp_UpdateExcelFile', 'P') IS NULL
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_UpdateExcelFile]
        @Id UNIQUEIDENTIFIER,
        @FilePath NVARCHAR(1000) = NULL,
        @Status TINYINT,
        @ProcessedAt DATETIME2 = NULL,
        @Periodo DATE = NULL
    AS
    BEGIN
        UPDATE ExcelFiles
        SET
            FilePath = ISNULL(@FilePath, FilePath),
            Status = @Status,
            ProcessedAt = ISNULL(@ProcessedAt, ProcessedAt),
            Periodo = ISNULL(@Periodo, Periodo)
        WHERE Id = @Id;
    END
    ')
END
GO

PRINT '✅ Base de datos, tablas y SPs inicializados correctamente.';
GO
