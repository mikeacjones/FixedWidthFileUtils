# About

Tool for serializing objects to fixed width files, or deserializing fixed width files to objects.

Some rules to keep in mind:

* Position of the `FixedField` defines the order of the value rather than the start index of the substring
	- EG: `00001   Michael`: though the name field starts at character index 5, its position is the second element, position would be 1 (elements use a 0 index position)
* Value types can not be directly serialized / deserialized (ie: `FixedWidthSerializer.Deserialize<int>("00001");` is not valid)
* All properties must be decorated with `FixedField`, with a minimum of the position provided
* An object can contains value types, or complex types, not both
* Each object containing value types must be its own line

# Examples

## Serialization

```csharp
public class Person
{
	//field at position 0 is 10 long, pad with spaces, and align the text to the left side of the field (Right is default)
	//NOTE: Field position defines the order of fields, rather than the index that the substring of the field starts at
	[FixedField(0, 10, ' ', FixedFieldAlignment.Left, FixedFieldOverflowMode.Truncate)] //align field to the left, and just truncate if their name is too long - their ID is the import bit!
	public string FirstName { get; set; }
	
	[FixedField(1, 10, ' ', FixedFieldAlignment.Left, FixedFieldOverflowMode.Truncate)]
	public string LastName { get; set; }

	[FixedField(2, 10)] //default is right aligned and padding with 0's
	public int ID { get; set; }
}

Person p = new Person
{
	
	FirstName = "Mike",
	LastName = "Jones",
	ID = 1234
};

string result = FixedWidthSerializer.Serialize(Person);
//Mike      Jones     0000001234
```

## Deserialization

```csharp
//sample file contains:
/*
Mike      Jones     0000001234
Bill      Jones     0000001235
*/
List<Person> people = null;
using (FileStream fs = new FileStream(sampleFile))
	FixedWidthSerializer.Deserialize<List<Person>>(fs);
if (people != null)
{
	foreach (var person in people)
		Console.WriteLine($"{person.LastName}, {person.FirstName} has an ID of {person.ID}");
}
```

## A bit more complex..

Mixing value fields and complex (class) fields is not supported - you must nest instead. For example, Wells Fargo requires check records to be grouped by account number and include a trailer. You can achieve this by building your models like so:

```csharp
public class CheckGroup
{
	[FixedField(0)]
	public CheckRecord[] Records { get; set; }

	[FixedField(1)]
	public CheckGroupTrailer Trailer { get; set; }
}
public class CheckRecord
{
	[FixedField(0, 10)]
	public long CheckSerial { get; set; }

	[FixedField(1, 6)]
	[FixedFieldSerializer(typeof(WellsFargoDateSerializer))]
	public DateTime IssueDate { get; set; }

	[FixedField(2, 15)]
	public long AccountNumber { get; set; }

	[FixedField(3, 3)]
	public int TransactionCode => 320;

	[FixedField(4, 10)]
	[FixedFieldSerializer(typeof(DecimalToPenniesSerializer))]
	public decimal Amount { get; set; }

	[FixedField(5, 41, ' ', FixedFieldAlignment.Left)]
	public string Payee { get; set; }
}
[FixedObjectPattern("^&")]
public class CheckGroupTrailer
{
	[FixedField(0, 15, ' ', FixedFieldAlignment.Left)]
	public string Start => "&";

	[FixedField(1, 5)]
	public int RecordCount { get; set; }

	[FixedField(2, 3, ' ')]
	[FixedField(4, 52, ' ')] //a property can be used multiple times in an object's string. Mostly useful for placeholder properties
	public string Spacer => string.Empty;

	[FixedField(3, 10)]
	[FixedFieldSerializer(typeof(DecimalToPenniesSerializer))]
	public decimal TotalAmount { get; set; }
}
```

Once you build your models like this, you could then Deserialize the following input:

```
000000604201292000000XXXXXXXXXX3200000016809WELLS FARGO BANK N.A.                    
&              00001   0000016809                                                    
000001998201292000000XXXXXXXXXX3200000340683SGS NORTH AMERICA                        
&              00001   0000340683                                                    
000003667501292000000XXXXXXXXXX3200000382792SAMS, LARKIN & HUFF                      
00000366760129200000XXXXXXXXXXX3200000352979HARKLEROAD & ASSOC., INC.                
000003667701292000000XXXXXXXXXX3200000022175WELLS FARGO BANK N.A.                    
&              00003   0000757946                                                    
000002372401292000000XXXXXXXXXX3200000243300SAMS, LARKIN & HUFF                      
000002372501292000000XXXXXXXXXX3200002900000C&M EQUIPMENT                            
000002372601292000000XXXXXXXXXX3200000079753CONSOLIDATED ELECTRICAL DIST., INC.      
000002372701292000000XXXXXXXXXX3200000012500EPIC PARTNERS INSURANCE CENTER           
000002372801292000000XXXXXXXXXX3200000091500ROLL-A-SHADE, INC.                       
000002372901292000000XXXXXXXXXX3200001846417SUNSHINE ELECTRONIC DISPLAY              
000002373001292000000XXXXXXXXXX3200000021267WELLS FARGO BANK N.A.                    
&              00007   0005194737                                                    
```

With a simple call:

```csharp
public static void Main()
{
	string inputString = "...";
	CheckGroup[] checkGroups = FixedWidthSerializer.Deserialize<CheckGroup[]>(inputString);

	//and then serialize it again...

	string fixedWidthFileContent = FixedWidthSerializer.Serialize(checkGroups);
}
```

## Custom Serializer
```csharp
public class WellsFargoDateSerializer : FixedFieldSerializer<DateTime>
{
	public override DateTime Deserialize(string input) => DateTime.ParseExact(input, "MMddyy", CultureInfo.InvariantCulture);
	public override string Serialize(DateTime input) => input.ToString("MMddyy");
}

public class SampleObject
{
	[FixedField(0, 6)]
	[FixedFieldSerializer(typeof(WellsFargoDateSerializer))]
	public DateTime IssueDate { get; set; }
}

var sample = new SampleObject
{
	IssueDate = DateTime.Now
};

Console.WriteLine(FixedWidthSerializer.Serialize(sample)); //012920

Console.WriteLine(FixedWidthSerializer.Deserialize<SampleObject>("012920").IssueDate); //1/29/2020 12:00:00 AM
```

## Object Pattern Indicators
Often times, all lines in a file are the same width. This is not an issue if you aren't dealing with nested objects with collections, but it can present some issues when that is the case. 
If you need to provide a regular expression indicator to differentiate object types, you can do so like so:

```csharp
[FixedObjectPattern("^&")] //trailer always starts with &, and is only line type which does so.
public class Trailer
{
	...
}
```