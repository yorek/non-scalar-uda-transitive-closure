USE tempdb
GO

/*
Create database
*/
IF DB_ID('TransitiveClosure') IS NOT NULL BEGIN
	ALTER DATABASE [TransitiveClosure] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [TransitiveClosure];
END
CREATE DATABASE [TransitiveClosure]
GO
USE [TransitiveClosure]
GO

/*
Enable SQLCLR on the database
*/
EXEC sp_configure 'clr enabled' , '1';  
RECONFIGURE;
GO
EXEC sp_configure 'show advanced option', '1';  
RECONFIGURE
GO


/*
For SQL Server 2017
*/
EXEC sp_configure 'clr strict security', '0'
RECONFIGURE WITH OVERRIDE
GO


/*
Cleanup if needed
*/
DROP AGGREGATE IF EXISTS TCC
GO
DROP ASSEMBLY IF EXISTS [TransitiveClosure]
GO

/*
Create assembly and UDA
NOTE: make sure permission on the file let SQL Server to access it
*/
CREATE ASSEMBLY [TransitiveClosure]
FROM 'd:\Work\_github\non-scalar-uda-transitive-closure\TransitiveClosureAggregatorLibrary\bin\Release\TransitiveClosureAggregatorLibrary.dll'
WITH PERMISSION_SET=SAFE
GO

CREATE AGGREGATE TCC(@id1 INT, @id2 INT)  
RETURNS NVARCHAR(MAX)  
EXTERNAL NAME [TransitiveClosure].[TransitiveClosure.Aggregate];  
GO
