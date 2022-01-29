
# ChangeTracker

## Purpose
To keep track of changes in properties of registered objects.

## Means
Instance of an object have to be registered in an instance of **ChangeTracker**. On registration, values of registering object properties are saved and either restored at invocation of **RejectChanges** method, either updated at invocation of **AcceptChanges** method.  
The state of registered object can be acquired with **IsDirty** method and changed properties can be acquired with **GetDirtyProperties** method.  
Items which properties had changed and a list of changed properties can be acquired with **DirtyItmes** enumeration.  
Properties which are marked with **ChangeTrackerIgnore** attribute will be excluded from change tracking.  
Properties marked with **ChangeTrackerCollection** attribute will be treated as collections and their values will also be saved.

## Details
- Only the properties which has public getter and setter can be tracked
- Only the properties which instances implements IList or ICollection\<T\> can be threated as value collections

## Example
Sample class:  

	class Foo
	{
		private static int _couter;

		public Foo()
		{
			Id = _couter++;
		}
    
		//this property will be ignored, as it read only
		public int Id { get; private set; }

		public int Number { get; set; }

		public string Text { get; set; }
    
		public object Reference { get; set; }

		//this property will be ignored even if listed in constructor parameters
		[ChangeTrackerIgnore] 
		public decimal Ignore { get; set; }
    
		//this property well be treated as a value collection, its values will be saved and restored as well
		[ChangeTrackerCollection]
		public int[] Array { get; set; }
    
		//this property will also be treated as a value collection, but the instance of collection won't be saved or restored
		//if at call of RejectChanges the property's value will be null, an exception will be thrown
		[ChangeTrackerCollection(storeCollectionInstance: false)]
		public ICollection<int> Collection { get; set; }
    
		//this property will be ignored, as it has only the getter
		public int Getter { get; } = -1;
	}

At constructor we can give list of properties to be tracked and comparers to be used while detecting changes. The properties of object will be processed according the given order.

	var properties = new[]
	{
		nameof(Foo.Number),
		nameof(Foo.Text),
		nameof(Foo.Collection),
		nameof(Foo.Array),
		nameof(Foo.Reference)
	}
	.Join(typeof(Foo).GetProperties(), name => name, pInfo => pInfo.Name, (name, pInfo) => pInfo)
	.ToList();

	//note that the key have to be one of the instances of PropertyInfo given in 'properties' parameter
	var comparers = new Dictionary<PropertyInfo, IEqualityComparer>()
	{
		{
			properties.First(pInfo => pInfo.Name.Equals(nameof(Foo.Text))),
			StringComparer.InvariantCultureIgnoreCase
		}
	};

	var changeTracker = new ChangeTracker<Foo>(properties, comparers);

If there are some items which have to be skipped on processing, we can do that like this.  
Let's say, we want to skip some items at value recovery:

	var lockedItems = new HashSet<Foo>();
	changeTracker.OnRejectChanges += (sender, a) => 
	{
		a.Cancel = lockedItems.Contains(a.Item);
	};

If in some cases the order of processed properties have to be changed, we can do it like this.  
Let's say, for some items we want to save the 'Reference' property first:

	var saveReferenceFirst = new HashSet<Foo>();
	changeTracker.OnAcceptChanges += (sender, a) =>
	{
		if (saveReferenceFirst.Contains(a.Item))
		{
			var refProperty = a.DirtyPropertiesSource.FirstOrDefault(p => p.Name.Equals(nameof(Foo.Reference)));

			if (refProperty != null)
			{
				a.DirtyProperties.Remove(refProperty);
				a.DirtyProperties.Insert(0, refProperty);
			}
		}
	};

Example of work:

	var item = new Foo()
	{
		Number = 100,
		Text = "ABCD",
		Array = new[] { 1, 2, 3, 4 },
		Collection = new List<int>() { 1, 2, 3, 4 }
	};

	changeTracker.Add(item);

	item.Number = 100;
	Console.WriteLine(changeTracker.IsDirty(item));
	//false

	item.Text = "abcd";
	Console.WriteLine(changeTracker.IsDirty(item));
	//false, as comparer for the 'Text' property is not case sensetive

	item.Text = "DCBA";
	Console.WriteLine(changeTracker.IsDirty(item));
	//true

	changeTracker.RejectChanges();

	Console.WriteLine(changeTracker.IsDirty(item));
	Console.WriteLine(item.Text);
	//false
	//ABCD

	item.Array[2] = 99;
	Console.WriteLine(string.Join(", ", changeTracker.GetDirtyProperties(item).Select(pInfo => pInfo.Name)));
	//Array

	var arrayInstance = item.Array;
	item.Array = null;

	changeTracker.RejectChanges();

	Console.WriteLine(arrayInstance.Equals(item.Array));
	Console.WriteLine(string.Join(", ", item.Array));
	//true
	//1, 2, 3, 4

	item.Array[2] = 99;
	changeTracker.AcceptChanges();
	
	Console.WriteLine(changeTracker.IsDirty(item));
	Console.WriteLine(string.Join(", ", item.Array));
	//false
	//1, 2, 99, 4
	
	item.Collection.Clear();
	Console.WriteLine(changeTracker.IsDirty(item));
	//true

	item.Collection = new int[] { 1, 2, 3, 4 };
	Console.WriteLine(changeTracker.IsDirty(item));
	//false, as for the 'Collection' property only the values of collection are tracked

	item.Collection = new int[] { 4, 3, 2, 1 };
	Console.WriteLine(changeTracker.IsDirty(item));
	//false, as the 'Collection' property is declared as ICollection<int>, and ICollection<T> does not implies the exact order of items
