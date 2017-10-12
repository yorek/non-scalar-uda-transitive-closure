USE TransitiveClosure
GO

-- Sample data
SET NOCOUNT ON;
GO

DROP TABLE IF EXISTS dbo.T1;
GO
CREATE TABLE dbo.T1
(
  id1 INT NOT NULL,
  id2 INT NOT NULL,
  CONSTRAINT PK_T1 PRIMARY KEY (id1, id2),
  CONSTRAINT CHK_T1_id1_LT_id2 CHECK (id1 < id2) -- since graph is undirected no need to keep both (x, y) and (y, x)
);
GO

CREATE INDEX ix ON dbo.T1 (id1, id2)
GO

--INSERT INTO dbo.T1 VALUES 
--(1, 2),
--(3, 4),
--(2, 3),
--(24, 25),
--(89, 90),
--(17, 24),
--(18, 24),
--(18, 25),
--(17, 18);
--GO

DECLARE @BinCount AS INT = 1;
DECLARE @BinSize AS INT = 100000;
DECLARE @MaxValueInBin AS INT = @BinSize + 1;

-- Create @BinCount bins each one of size @BinSize
DECLARE @J INT = 1;
WHILE(@J<=@BinCount) 
BEGIN
	PRINT @J;
	DECLARE @I INT = 0;
	WHILE(@I<@BinSize) 
	BEGIN		
		DECLARE @id1 INT = @BinSize * @J + CAST(RAND()*@MaxValueInBin AS INT);
		DECLARE @id2 INT = @BinSize * @J + CAST(RAND()*@MaxValueInBin AS INT);	
		IF (@id1 != @id2)
		BEGIN
			IF (@id2 < @id1) SELECT @id1=id2, @id2=id1 FROM (VALUES(@id1, @id2)) AS T(id1, id2);
			INSERT INTO dbo.T1 SELECT @id1, @id2 FROM (VALUES(1)) AS T(n) WHERE NOT EXISTS (SELECT * FROM dbo.T1 WHERE (id1 = @id1 and id2 = @id2));
			SET @I += @@ROWCOUNT;
		END
	END
	SET @J += 1;
END
GO

-- Total number of pairs
SELECT PairsCount = COUNT(*) FROM dbo.T1;

-- Count of distict single elements
SELECT UniqueIdsCount = COUNT(*) FROM (
	SELECT id1 FROM dbo.T1
	UNION
	SELECT id2 FROM dbo.t1
) T

--SELECT * FROM dbo.T1


/*
 
DROP TABLE IF EXISTS dbo.T1;
DROP FUNCTION IF EXISTS dbo.GetNums;
GO
 
-- Helper function dbo.GetNums
CREATE FUNCTION dbo.GetNums(@low AS BIGINT, @high AS BIGINT) RETURNS TABLE
AS
RETURN
  WITH
    L0   AS (SELECT c FROM (SELECT 1 UNION ALL SELECT 1) AS D(c)),
    L1   AS (SELECT 1 AS c FROM L0 AS A CROSS JOIN L0 AS B),
    L2   AS (SELECT 1 AS c FROM L1 AS A CROSS JOIN L1 AS B),
    L3   AS (SELECT 1 AS c FROM L2 AS A CROSS JOIN L2 AS B),
    L4   AS (SELECT 1 AS c FROM L3 AS A CROSS JOIN L3 AS B),
    L5   AS (SELECT 1 AS c FROM L4 AS A CROSS JOIN L4 AS B),
    Nums AS (SELECT ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS rownum
             FROM L5)
  SELECT TOP(@high - @low + 1) @low + rownum - 1 AS n
  FROM Nums
  ORDER BY rownum;
GO
 
CREATE TABLE dbo.T1
(
  id1 INT NOT NULL,
  id2 INT NOT NULL,
  CONSTRAINT PK_T1 PRIMARY KEY (id1, id2),
  CONSTRAINT CHK_T1_id1_LT_id2 CHECK (id1 < id2) -- since graph is undirected no need to keep both (x, y) and (y, x)
);
GO
 
DECLARE @n AS INT = 100000; -- num rows
 
INSERT INTO dbo.T1(id1, id2)
  SELECT n AS id1,
    n + ABS(CHECKSUM(NEWID())) % (@n + 1 - n) + 1 AS id2
  FROM dbo.GetNums(1, @n);
GO
*/