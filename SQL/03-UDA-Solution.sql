USE [TransitiveClosure]
GO

-- Simple test, will return a JSON
select dbo.TCC(id1, id2) from dbo.TestDataSmall
go

-- Extract all group, with their JSON array element
with cte as 
(
	select dbo.TCC(id1, id2) as R from dbo.TestDataSmall
)
select 
	J1.[key] as GroupId,
	J1.[value]
from 
	cte
cross apply
	openjson(cte.R) J1
go

-- Fully decode JSON
with cte as 
(
	select dbo.TCC(id1, id2) as R from dbo.TestDataSmall
)
select 
	cast(J1.[key] as int)  as GroupId,
	cast(J2.value as int) as Id
from 
	cte
cross apply
	openjson(cte.R) J1
cross apply
	openjson(J1.value) J2
order by
	GroupId, Id
;

