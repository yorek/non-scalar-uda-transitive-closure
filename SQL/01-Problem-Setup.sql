USE [TransitiveClosure]
GO

-- Sample data
SET NOCOUNT ON;
GO

DROP TABLE IF EXISTS dbo.TestDataSmall;
DROP TABLE IF EXISTS dbo.TestDataBig;
GO
CREATE TABLE dbo.TestDataSmall
(
  id1 INT NOT NULL,
  id2 INT NOT NULL,
  CONSTRAINT PK_TestDataSmall PRIMARY KEY (id1, id2),
  CONSTRAINT CHK_TestDataSmall CHECK (id1 < id2) -- since graph is undirected no need to keep both (x, y) and (y, x)
);
GO
CREATE TABLE dbo.TestDataBig
(
  id1 INT NOT NULL,
  id2 INT NOT NULL,
  CONSTRAINT PK_TestDataBig PRIMARY KEY (id1, id2),
  CONSTRAINT CHK_TestDataBig CHECK (id1 < id2) -- since graph is undirected no need to keep both (x, y) and (y, x)
);
GO
CREATE INDEX ix ON dbo.TestDataSmall (id1, id2)
GO
CREATE INDEX ix ON dbo.TestDataBig (id1, id2)
GO

-- Easy test values
INSERT INTO dbo.TestDataSmall VALUES 
(1, 2),
(3, 4),
(2, 3),
(24, 25),
(89, 90),
(17, 24),
(18, 24),
(18, 25),
(17, 18);
GO

/*
Helper function dbo.GetNums
*/
DROP FUNCTION IF EXISTS dbo.GetNums;
GO

CREATE FUNCTION dbo.GetNums(@low AS BIGINT, @high AS BIGINT) RETURNS TABLE
AS RETURN
WITH
	L0 AS (SELECT c FROM (SELECT 1 UNION ALL SELECT 1) AS D(c)),
	L1 AS (SELECT 1 AS c FROM L0 AS A CROSS JOIN L0 AS B),
	L2 AS (SELECT 1 AS c FROM L1 AS A CROSS JOIN L1 AS B),
	L3 AS (SELECT 1 AS c FROM L2 AS A CROSS JOIN L2 AS B),
	L4 AS (SELECT 1 AS c FROM L3 AS A CROSS JOIN L3 AS B),
	L5 AS (SELECT 1 AS c FROM L4 AS A CROSS JOIN L4 AS B),
	F AS (SELECT ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS rownum FROM L5)
SELECT 
	TOP(@high - @low + 1) @low + rownum - 1 AS n
FROM 
	F
ORDER BY 
	rownum;
GO

/*
Generate data
*/
DECLARE @groups AS INT = 2000;
DECLARE @rowsPerGroup AS INT = 100;

INSERT INTO dbo.TestDataBig(id1, id2)
SELECT 
	--G.n,
	x AS id1,
    x + ABS(CHECKSUM(NEWID())) % (@rowsPerGroup + 1 - R.n) + 1 AS id2
FROM 
	dbo.GetNums(1, @groups) AS G
CROSS JOIN 
	dbo.GetNums(1, @rowsPerGroup) AS R
CROSS APPLY 
	( VALUES( (G.n - 1) * (@rowsPerGroup + 1) + R.n ) ) AS D(x)
ORDER BY 
	id1, id2;


