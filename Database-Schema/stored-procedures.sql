-- stored-procedures.sql
-- All stored procedures for the Expense Management app
-- Run via run-sql-stored-procs.py

CREATE OR ALTER PROCEDURE dbo.sp_GetExpenses
    @StatusFilter NVARCHAR(50) = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId, e.UserId, u.UserName,
        e.CategoryId, c.CategoryName,
        e.StatusId, s.StatusName,
        e.AmountMinor, e.Currency, e.ExpenseDate,
        e.Description, e.ReceiptFile,
        e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE (@StatusFilter IS NULL OR s.StatusName = @StatusFilter)
      AND (@UserId IS NULL OR e.UserId = @UserId)
    ORDER BY e.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId, e.UserId, u.UserName,
        e.CategoryId, c.CategoryName,
        e.StatusId, s.StatusName,
        e.AmountMinor, e.Currency, e.ExpenseDate,
        e.Description, e.ReceiptFile,
        e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DraftStatusId INT;
    SELECT @DraftStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile, CreatedAt)
    VALUES (@UserId, @CategoryId, @DraftStatusId, @AmountMinor, 'GBP', @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateExpenseStatus
    @ExpenseId INT,
    @StatusId INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET StatusId = @StatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SubmitExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SubmittedStatusId INT;
    SELECT @SubmittedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';
    UPDATE dbo.Expenses
    SET StatusId = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Expenses WHERE ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.UserName, u.Email, u.RoleId, r.RoleName, u.ManagerId, u.IsActive
    FROM dbo.Users u
    JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.UserName, u.Email, u.RoleId, r.RoleName, u.ManagerId, u.IsActive
    FROM dbo.Users u
    JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.UserId = @UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive FROM dbo.ExpenseCategories WHERE IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetStatuses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusId, StatusName FROM dbo.ExpenseStatus;
END
GO
