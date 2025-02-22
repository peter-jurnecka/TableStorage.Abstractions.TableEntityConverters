﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using TableStorage.Abstractions.TableEntityConverters;
using Xunit;

namespace TableStorage.Abstractions.UnitTests
{
	public class EntityConvertTests
	{
		public EntityConvertTests()
		{
			EntityConvert.SetDefaultJsonSerializerOptions();
		}
		
		public class GuidKeyTest
		{
			public Guid A { get; set; }
			public Guid B { get; set; }
		}


		[Fact]
		public void convert_from_entity_table()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_with_timestamp()
		{
			var emp = new EmployeeWithTimestamp
            {
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			tableEntity.Timestamp = DateTime.UtcNow;
			var employee = tableEntity.FromTableEntity<EmployeeWithTimestamp, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
			Assert.Equal(tableEntity.Timestamp, employee.Timestamp);
		}

		[Fact]
		public void convert_from_entity_table_with_timestamp_as_string()
		{
			var emp = new EmployeeWithTimestampAsString
            {
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			tableEntity.Timestamp = DateTime.UtcNow;
			var employee = tableEntity.FromTableEntity<EmployeeWithTimestampAsString, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
			Assert.Equal(tableEntity.Timestamp.ToString(), employee.Timestamp);
		}

		[Fact]
		public void convert_from_entity_table_with_timestamp_as_datetime()
		{
			var emp = new EmployeeWithTimestampAsString
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			tableEntity.Timestamp = DateTime.UtcNow;
			var employee = tableEntity.FromTableEntity<EmployeeWithTimestampAsDateTime, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
			Assert.Equal(DateTimeKind.Utc, employee.Timestamp.Kind);
			Assert.Equal(tableEntity.Timestamp, employee.Timestamp);
		}
		
		[Fact]
		public void convert_from_entity_table_complex_key()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity($"company_{emp.Company}", $"employee_{emp.Id}");
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company,
				pk => pk.Substring("company_".Length), e => e.Id, rk => int.Parse(rk.Substring("employee_".Length)));

			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_unmapped_partition_key()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var te = emp.ToTableEntity($"company_{emp.Company}", $"employee_{emp.Id}");
			var employee = te.FromTableEntity<Employee, string, int>(null, null, e => e.Id,
				rk => int.Parse(rk.Substring("employee_".Length)));

			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_unmapped_partition_key_and_unmapped_row_key()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var te = emp.ToTableEntity($"company_{emp.Company}", $"employee_{emp.Id}");
			var employee = te.FromTableEntity<Employee>();

			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}


		[Fact]
		public void convert_from_entity_table_with_datetime()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				ADateTime = DateTime.Parse("Wednesday, January 31, 2018")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_with_guid_keys()
		{
			var a = Guid.Parse("7ba5bd25-823e-4c01-940e-1f131cbed8ed");
			var b = Guid.Parse("603e51de-950e-4270-a755-c26950742103");

			var obj = new GuidKeyTest {A = a, B = b};
			var e = obj.ToTableEntity(x => x.A, x => x.B);
			var convertedObject = e.FromTableEntity<GuidKeyTest, Guid, Guid>(x => x.A, x => x.B);


			Assert.Equal(a, convertedObject.A);
		}


		[Fact]
		public void convert_from_entity_table_with_nullable_datetime_with_value()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				ANullableDateTime = DateTime.Parse("Wednesday, January 31, 2018")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_from_entity_table_with_nullable_datetimeoffset_with_value()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				TermDate = DateTimeOffset.Parse("Wednesday, January 31, 2018")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}


		[Fact]
		public void convert_from_entity_table_with_nullable_int_with_value()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0")
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	"),
				ANullableDateTime = DateTime.Parse("Wednesday, January 31, 2018"),
				ANullableInt = 42
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);
			var employee = tableEntity.FromTableEntity<Employee, string, int>(e => e.Company, e => e.Id);
			Assert.Equal(Guid.Parse("12ae85a4-7131-4e8c-af63-074b066412e0"), employee.Department.OptionalId);
		}

		[Fact]
		public void convert_to_entity_table()
		{
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);

			Assert.True(tableEntity.Keys.Contains("DepartmentJson"));
			var dept = tableEntity.GetString("DepartmentJson").ToLowerInvariant();
			Assert.Contains("optionalid", dept);
		}
		
		[Fact]
		public void convert_to_entity_table_custom_json_settings_as_a_global_setting()
		{
			EntityConvert.SetDefaultJsonSerializerOptions(new JsonSerializerOptions {
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			});
			
			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id);

			var dept = tableEntity.GetString("DepartmentJson").ToLowerInvariant();
			Assert.DoesNotContain("optionalid", dept);
		}
		
		[Fact]
		public void convert_to_entity_table_custom_json_settings_as_a_local_setting()
		{
			var jsonSerializerSettings = new JsonSerializerOptions
			{
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			};


			var emp = new Employee
			{
				Company = "Microsoft",
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				Id = 42,
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id, jsonSerializerSettings);

			var dept = tableEntity.GetString("DepartmentJson").ToLowerInvariant();
			Assert.DoesNotContain("optionalid", dept);
		}
		
		
		[Fact]
		public void convert_to_entity_table_explicit_keys()
		{
			var emp = new Employee
			{
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity("Google", "42");
			Assert.Equal("Google", tableEntity.PartitionKey);
		}


		[Fact]
		public void convert_to_entity_table_ignore_complex_properties()
		{
			var emp = new Employee
			{
				Company = "Google",
				Name = "John Smith",
				Id = 1,
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id,e => e.Department);
			Assert.Equal("Google", tableEntity.PartitionKey);
			Assert.True(tableEntity.Keys.Contains("ExternalId"));
			Assert.True(tableEntity.Keys.Contains("HireDate"));
			Assert.False(tableEntity.Keys.Contains("DepartmentJson"));
		}


		[Fact]
		public void convert_to_entity_table_ignore_simple_properties()
		{
			var emp = new Employee
			{
				Company = "Google",
				Name = "John Smith",
				Id = 1,
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity(e => e.Company, e => e.Id, e => e.ExternalId, e => e.HireDate);
			Assert.Equal("Google", tableEntity.PartitionKey);
			Assert.False(tableEntity.Keys.Contains("ExternalId"));
			Assert.False(tableEntity.Keys.Contains("HireDate"));
		}

		[Fact]
		public void convert_to_entity_table_with_explicit_Keys_with_ignored_simple_properties()
		{
			var emp = new Employee
			{
				Name = "John Smith",
				Department = new Department
				{
					Name = "QA",
					Id = 1,
					OptionalId = null
				},
				ExternalId = Guid.Parse("e3bf64f4-0537-495c-b3bf-148259d7ed36"),
				HireDate = DateTimeOffset.Parse("Thursday, January 31, 2008	")
			};
			var tableEntity = emp.ToTableEntity("Google", "42", e => e.ExternalId, e => e.HireDate);
			Assert.Equal("Google", tableEntity.PartitionKey);
			Assert.False(tableEntity.Keys.Contains("ExternalId"));
			Assert.False(tableEntity.Keys.Contains("HireDate"));
		}

		[Fact]
		public void convert_to_entity_table_custom_serialized_property()
		{
			var car = new Car {
				Id = "abc",
				Make = "BMW",
				Model = "M5",
				Year = 2022,
				ReleaseDate = new DateTime(2022, 3, 1)
			};

			var propertyConverters = new PropertyConverters<Car> {
				[nameof(Car.ReleaseDate)] =
					new PropertyConverter<Car>(x => 
							car.ReleaseDate.ToString("yyyy-M-d"),
						(c,p) =>c.ReleaseDate = DateTime.Parse(p.ToString())
						)
			};
			var carEntity =
				car.ToTableEntity(c => c.Year, c => car.Id, new JsonSerializerOptions(), propertyConverters);
			Assert.Equal("2022-3-1", carEntity.GetString(nameof(car.ReleaseDate)));
		}
		
		[Fact]
		public void convert_from_entity_table_custom_serialized_property()
		{
			var car = new Car {
				Id = "abc",
				Make = "BMW",
				Model = "M5",
				Year = 2022,
				ReleaseDate = new DateTime(2022, 3, 1)
			};

			var propertyConverters = new PropertyConverters<Car> {
				[nameof(car.ReleaseDate)] =
					new(_ => 
							car.ReleaseDate.ToString("yyyy-M-d"),
						(c,p) =>c.ReleaseDate = DateTime.Parse(p.ToString())
					)
			};

			var jsonSerializerSettings = new JsonSerializerOptions();
			
			var carEntity =
				car.ToTableEntity(c => c.Year, c => car.Id, jsonSerializerSettings, propertyConverters);

			var fromEntity = carEntity.FromTableEntity<Car,int,string>(c=>c.Year, c=>c.Id, jsonSerializerSettings, propertyConverters);
			Assert.Equal(new DateTime(2022, 3, 1), fromEntity.ReleaseDate);
		}
	}
}