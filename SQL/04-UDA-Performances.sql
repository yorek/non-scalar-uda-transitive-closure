USE [TransitiveClosure]
GO

/*
Test performance on high cardinality table
*/

with cte as 
(
	select dbo.TCC(id1, id2) as R from dbo.TestDataBig
),
cte2 AS
(
	select 
		cast(J1.[key] as int)  as GroupId,
		cast(J2.value as int) as Id
	from 
		cte
	cross apply
		openjson(cte.R) J1
	cross apply
		openjson(J1.value) J2
)
SELECT
	GroupId,
	COUNT(*)
FROM
	cte2
GROUP BY
	cte2.GroupId
WITH 
	ROLLUP
ORDER BY
	GroupId  