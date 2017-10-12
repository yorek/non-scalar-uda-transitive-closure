/*
For SQL Server 2017
*/
exec sp_configure 'clr enabled' , '1';  
go
reconfigure;    
exec sp_configure 'show advanced option', '1';  
go
reconfigure
go
exec sp_configure 'clr strict security', '0'
go
reconfigure with override
go


drop aggregate if exists TCD
go

drop assembly if exists [TransitiveClosure]
go


create assembly [TransitiveClosure]
from 'd:\Work\_github\non-scalar-uda-transitive-closure\TransitiveClosureAggregatorLibrary\bin\Release\TransitiveClosureAggregatorLibrary.dll'
with permission_set=safe


create aggregate TCD(@id1 int, @id2 int)  
returns nvarchar(max)  
external name [TransitiveClosure].[TransitiveClosure.Aggregate];  
GO  

-- Simple test, will return a JSON
select dbo.TCD(id1, id2) from dbo.T1
go

-- Extract all group, with their JSON array element
with cte as 
(
	select dbo.TCD(id1, id2) as R from dbo.T1
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
	select dbo.TCD(id1, id2) as R from dbo.T1
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

-- Count elements in groups
-- Fully decode JSON
with cte as 
(
	select dbo.TCD(id1, id2) as R from dbo.T1
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
WITH ROLLUP
ORDER BY
	GroupId  