USE [TransitiveClosure]
GO

DROP TABLE IF EXISTS dbo.[Groups];
CREATE TABLE dbo.[Groups] (groupId INT, id INT);
GO

CREATE NONCLUSTERED INDEX ixnc ON dbo.[Groups](id)
GO

DROP SEQUENCE IF EXISTS dbo.[GroupsSequence];
CREATE SEQUENCE dbo.[GroupsSequence] AS INT START WITH 1;
GO

SET NOCOUNT ON;
DECLARE c CURSOR FORWARD_ONLY FOR
SELECT 
	ss.id1,
	ss.id2
FROM
	dbo.TestDataSmall ss
ORDER BY
	ss.id1, ss.id2
;

DECLARE @id1 INT, @id2 INT;
DECLARE @groupId INT, @groupId1 INT, @groupId2 INT;

OPEN c;

FETCH NEXT FROM c INTO @id1, @id2;

WHILE (@@FETCH_STATUS = 0)
BEGIN
	SET @groupId1 = NULL;
	SET @groupId2 = NULL;
	SET @groupId = NULL;

	SELECT @groupId1 = groupId FROM dbo.Groups WHERE id = @id1;
	SELECT @groupId2 = groupId FROM dbo.Groups WHERE id = @id2;

	-- Case 0: id1 or id2 has never been inserted before in any group
	IF(@groupId1 IS NULL AND @groupId2 IS NULL) BEGIN		
		SET @groupId = NEXT VALUE FOR [GroupsSequence];

		INSERT INTO dbo.Groups VALUES (@groupId, @id1)
		IF (@id1 != @id2) INSERT INTO dbo.Groups VALUES (@groupId, @id2)
	END
	
	-- Case 1: since id1 is already in one group and id2 not, put both in the same group
	IF(@groupId1 IS NOT NULL AND @groupId2 IS NULL) BEGIN
		INSERT INTO dbo.Groups VALUES (@groupId1, @id2)
	END

	-- Case 2: since id2 is already in one group and id1 not, put both in the same group
	IF(@groupId1 IS NULL AND @groupId2 IS NOT NULL) BEGIN
		INSERT INTO dbo.Groups VALUES (@groupId2, @id1)
	END

	-- Case 3: id1 is in one group and id2 is in another one. This could not be, so put both in the same group
	IF(@groupId1 IS NOT NULL AND @groupId2 IS NOT NULL AND @groupId1 != @groupId2) BEGIN
		UPDATE dbo.Groups SET groupId = @groupId1 WHERE groupId = @groupId2
	END
			
	FETCH NEXT FROM c INTO @id1, @id2;
END;

CLOSE c;

DEALLOCATE c;
GO

SELECT groupId, COUNT(*) AS [ids] FROM dbo.[Groups] GROUP BY groupId ORDER BY groupId
GO

SELECT * FROM dbo.[Groups] ORDER BY groupId
GO

