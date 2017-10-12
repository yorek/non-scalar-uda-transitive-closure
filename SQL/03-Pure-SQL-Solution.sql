SET NOCOUNT ON
GO

DROP TABLE IF EXISTS #G;
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
 
SELECT grp, COUNT(*) FROM #G GROUP BY grp WITH ROLLUP ORDER BY grp
GO

SELECT * FROM #G

DROP TABLE #G;