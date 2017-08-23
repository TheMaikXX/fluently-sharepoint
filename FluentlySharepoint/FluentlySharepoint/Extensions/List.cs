﻿using System;
using FluentlySharepoint.Models;
using Microsoft.SharePoint.Client;

namespace FluentlySharepoint.Extensions
{
	public static class List
	{
		public static CSOMOperation LoadList(this CSOMOperation operation, string name, Action<ClientContext, Microsoft.SharePoint.Client.List> listLoader = null)
		{
			var web = operation.DecideWeb();
			var list = web.Lists.GetByTitle(name);

			operation.Context.Load(web);
			if (listLoader != null)
				listLoader(operation.Context, operation.LastList);
			else
			{
				operation.Context.Load(list);
			}

			operation.SetLevel(OperationLevels.List, list);
			operation.ActionQueue.Enqueue(new DeferredAction { ClientObject = operation.LastList, Action = DeferredActions.Load });

			return operation;
		}

		public static CSOMOperation SelectList(this CSOMOperation operation, string name)
		{
			if (operation.LoadedLists.ContainsKey(name))
			{
				operation.SetLevel(OperationLevels.List, operation.LoadedLists[name]);
			}
			else
			{
				throw new ArgumentException($"List ${name} doesn't exist");
			}

			return operation;
		}

		public static CSOMOperation ChangeColumn(this CSOMOperation operation, string columnName, FieldType? type = null, string displayName = null, bool? required = null, bool? uniqueValues = null)
		{
			var field = operation.LastList.Fields.GetByInternalNameOrTitle(columnName);

			if (type.HasValue) field.TypeAsString = type.ToString();
			if (!String.IsNullOrEmpty(displayName)) field.Title = displayName;
			if (required.HasValue) field.Required = required.Value;
			if (uniqueValues.HasValue) field.EnforceUniqueValues = uniqueValues.Value;

			field.UpdateAndPushChanges(true);

			return operation;
		}

		public static CSOMOperation DeleteColumn(this CSOMOperation operation, string columnName)
		{
			var field = operation.LastList.Fields.GetByInternalNameOrTitle(columnName);
			field.DeleteObject();

			return operation;
		}

		public static CSOMOperation AddColumn(this CSOMOperation operation, string name, FieldType type, string displayName = "", bool required = false, bool uniqueValues = false)
		{
			FieldCreationInformation fieldInformation = new FieldCreationInformation
			{
				InternalName = name,
				DisplayName = String.IsNullOrEmpty(displayName) ? name : displayName,
				FieldType = type,
				Required = required,
				UniqueValues = uniqueValues
			};

			operation.LastList.Fields.AddFieldAsXml(fieldInformation.ToXml(), true, AddFieldOptions.AddFieldInternalNameHint | AddFieldOptions.AddFieldToDefaultView);

			return operation;
		}

		public static ListItemCollection GetItems(this CSOMOperation operation, string queryString)
		{
			var caml = new CamlQuery { ViewXml = queryString };

			return operation.GetItems(caml);
		}

		public static ListItemCollection GetItems(this CSOMOperation operation)
		{
			return GetItems(operation, CamlQuery.CreateAllItemsQuery());
		}

		public static ListItemCollection GetItems(this CSOMOperation operation, CamlQuery query)
		{
			var listItems = operation.LastList.GetItems(query);

			operation.Context.Load(listItems);
			operation.Execute();

			return listItems;
		}

		public static CSOMOperation DeleteItems(this CSOMOperation operation)
		{
			var caml = CamlQuery.CreateAllItemsQuery();

			operation.DeleteItems(caml);

			return operation;
		}

		public static CSOMOperation DeleteItems(this CSOMOperation operation, string queryString)
		{
			var caml = new CamlQuery { ViewXml = queryString };

			operation.DeleteItems(caml);

			return operation;
		}

		public static CSOMOperation DeleteItems(this CSOMOperation operation, CamlQuery query)
		{
			var items = operation.LastList.GetItems(query);

			operation.Context.Load(items);
			operation.ActionQueue.Enqueue(new DeferredAction { ClientObject = items, Action = DeferredActions.Delete });

			return operation;
		}

		public static CSOMOperation CreateList(this CSOMOperation operation, string name, string template = null)
		{
			ListCreationInformation listInformation = new ListCreationInformation
			{
				Title = name,
				ListTemplate = String.IsNullOrEmpty(template)
					? operation.LastWeb.ListTemplates.GetByName("Custom List")
					: operation.LastWeb.ListTemplates.GetByName(template)
			};

			var list = operation.LastWeb.Lists.Add(listInformation);

			operation.LastWeb.Context.Load(list);
			operation.SetLevel(OperationLevels.List, list);
			operation.ActionQueue.Enqueue(new DeferredAction{ClientObject = list, Action = DeferredActions.Load});

			return operation;
		}

		public static CSOMOperation DeleteList(this CSOMOperation operation, string name)
		{
			var list = operation.LastWeb.Lists.GetByTitle(name);

			operation.ActionQueue.Enqueue(new DeferredAction { ClientObject = list, Action = DeferredActions.Delete });

			return operation;
		}
	}
}