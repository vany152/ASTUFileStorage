begin;

create schema if not exists public;
set search_path to public;



------ create auxiliary functions
---- common
create function string_not_empty(str text)
    returns boolean
as
$$
begin
    return not (str is null or trim(str) = '');
end;
$$ language plpgsql;



-- create tables
create table files
(
    id uuid primary key,
    
    hash text unique,
    path text unique,
    
    uploader_id uuid not null,
    
    name text,
    
    links_count integer default 0,
    
    constraint non_empty_hash
        check (string_not_empty(hash)),
    
    constraint non_empty_path
        check (string_not_empty(path)),
    
    constraint non_empty_name
        check (string_not_empty(name)),
    
    constraint non_negative_links_count
        check (links_count >= 0)
);



commit;