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

-- If need to enforce id1 < id2 even if data entered has id2 < id1, can do this with an instead of trigger
DROP TRIGGER IF EXISTS trg_T1_IOINS_id1_LT_id2;
GO
CREATE OR ALTER TRIGGER trg_T1_IOINS_id1_LT_id2 ON dbo.T1 INSTEAD OF INSERT
AS

IF @@ROWCOUNT = 0 RETURN;
SET NOCOUNT ON;

INSERT INTO dbo.T1(id1, id2)
  SELECT
    CASE WHEN id1 < id2 THEN id1 ELSE id2 END AS id1,
    CASE WHEN id1 < id2 THEN id2 ELSE id1 END AS id2
  FROM inserted;
GO

INSERT INTO dbo.T1 VALUES 
(1, 2),
(3, 4),
(2, 3),
(25, 24),
(90, 89),
(17, 24),
(18, 24),
(18, 25),
(18, 17);
GO

DECLARE @J INT = 1;
WHILE(@J<=10) 
BEGIN
	DECLARE @I INT = 0;
	WHILE(@I<1000) 
	BEGIN		
		DECLARE @id1 INT = 100 * @J + CAST(RAND()*100 AS INT);
		DECLARE @id2 INT = 100 * @J + CAST(RAND()*100 AS INT);	
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

--select * from dbo.T1

