
-- Large set of sample data
SET NOCOUNT ON;
USE tempdb;

DROP TABLE IF EXISTS dbo.T1;
DROP FUNCTION IF EXISTS dbo.GetNums;
GO

-- Helper function dbo.GetNums
CREATE FUNCTION dbo.GetNums(@low AS BIGINT, @high AS BIGINT) RETURNS TABLE
AS
RETURN
  WITH
    L0   AS (SELECT c FROM (SELECT 1 UNION ALL SELECT 1) AS D(c)),
    L1   AS (SELECT 1 AS c FROM L0 AS A CROSS JOIN L0 AS B),
    L2   AS (SELECT 1 AS c FROM L1 AS A CROSS JOIN L1 AS B),
    L3   AS (SELECT 1 AS c FROM L2 AS A CROSS JOIN L2 AS B),
    L4   AS (SELECT 1 AS c FROM L3 AS A CROSS JOIN L3 AS B),
    L5   AS (SELECT 1 AS c FROM L4 AS A CROSS JOIN L4 AS B),
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

-- Faster solution
SET NOCOUNT ON;
USE tempdb;
GO

CREATE TABLE #G
(
  id INT NOT NULL,
  grp INT NOT NULL,
  lvl INT NOT NULL,
  PRIMARY KEY NONCLUSTERED (id),
  UNIQUE CLUSTERED(lvl, id)
);

DECLARE @lvl AS INT = 1, @added AS INT;

INSERT INTO #G(id, grp, lvl)
  SELECT A.id, A.grp, @lvl AS lvl
  FROM ( SELECT TOP (1) id1, id2
         FROM dbo.T1
         ORDER BY id1, id2 ) AS D
    CROSS APPLY ( VALUES(id1, id1),(id2, id1) ) AS A(id, grp);

SET @added = @@ROWCOUNT;

WHILE @added > 0
BEGIN
  WHILE @added > 0
  BEGIN
    SET @lvl += 1;
    
    INSERT INTO #G(id, grp, lvl)
      SELECT DISTINCT T1.id2 AS id, G.grp, @lvl AS lvl
      FROM #G AS G
        INNER JOIN dbo.T1
          ON G.id = T1.id1
      WHERE lvl = @lvl - 1
        AND NOT EXISTS
          ( SELECT * FROM #G AS G
            WHERE G.id = T1.id2 )
      
    SET @added = @@ROWCOUNT;
  
    INSERT INTO #G(id, grp, lvl)
      SELECT DISTINCT T1.id1 AS id, G.grp, @lvl AS lvl
      FROM #G AS G
        INNER JOIN dbo.T1
          ON G.id = T1.id2
      WHERE lvl = @lvl - 1
        AND NOT EXISTS
          ( SELECT * FROM #G AS G
            WHERE G.id = T1.id1 );            
  
    SET @added += @@ROWCOUNT;
  END;

  INSERT INTO #G(id, grp, lvl)
    SELECT A.id, A.grp, @lvl AS lvl
    FROM ( SELECT TOP (1) id1, id2
           FROM dbo.T1
           WHERE NOT EXISTS
             ( SELECT * FROM #G AS G
               WHERE G.id = T1.id1 )
           ORDER BY id1, id2 ) AS D
      CROSS APPLY ( VALUES(id1, id1),(id2, id1) ) AS A(id, grp);

  SET @added = @@ROWCOUNT;
END;

SELECT id, grp
FROM #G  

DROP TABLE #G;
