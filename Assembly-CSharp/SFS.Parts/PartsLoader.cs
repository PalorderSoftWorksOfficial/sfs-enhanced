using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts;

public class PartsLoader : MonoBehaviour
{
	public Transform detacher;

	public Material partMaterial;

	public Material partModelMaterialTexture;

	public Material partModelMaterialNormals;

	public Dictionary<string, ColorTexture> colorTextures;

	public Dictionary<string, ShapeTexture> shapeTextures;

	public Dictionary<string, Part> parts;

	public Dictionary<string, VariantRef> partVariants;

	private async void Awake()
	{
		colorTextures = ResourcesLoader.GetFiles_Dictionary<ColorTexture>("");
		shapeTextures = ResourcesLoader.GetFiles_Dictionary<ShapeTexture>("");
		(Dictionary<string, Part> loadedParts, Dictionary<string, VariantRef> loadedVariants) tuple = LoadPartVariants();
		Dictionary<string, Part> item = tuple.loadedParts;
		Dictionary<string, VariantRef> item2 = tuple.loadedVariants;
		parts = item;
		partVariants = item2;
		if (DevSettings.HasModLoader)
		{
			await CustomAssetsLoader.LoadAllCustomAssets();
		}
	}

	public static (Dictionary<string, Part> loadedParts, Dictionary<string, VariantRef> loadedVariants) LoadPartVariants()
	{
		Dictionary<string, Part> dictionary = LoadParts();
		Dictionary<string, VariantRef> dictionary2 = new Dictionary<string, VariantRef>();
		foreach (Part value in dictionary.Values)
		{
			for (int i = 0; i < value.variants.Length; i++)
			{
				for (int j = 0; j < value.variants[i].variants.Length; j++)
				{
					VariantRef variantRef = new VariantRef(value, i, j);
					dictionary2.Add(variantRef.GetNameID(), variantRef);
				}
			}
		}
		return (loadedParts: dictionary, loadedVariants: dictionary2);
	}

	public static Dictionary<string, Part> LoadParts()
	{
		List<Part> list = ResourcesLoader.GetFiles_Array<Part>("Parts").ToList();
		string[] disableParts = DevSettings.DisableParts;
		foreach (string text in disableParts)
		{
			if (text != "")
			{
				Part[] files_Array = ResourcesLoader.GetFiles_Array<Part>("Parts/" + text);
				foreach (Part item in files_Array)
				{
					list.Remove(item);
				}
			}
		}
		return list.ToDictionary((Part a) => a.name, (Part a) => a);
	}

	public static Part CreatePart(VariantRef variant, bool updateAdaptation)
	{
		return CreatePart(variant.part, null, delegate(Part createdPart)
		{
			if (variant.variantIndex_A != -1)
			{
				createdPart.variants[variant.variantIndex_A].variants[variant.variantIndex_B].ApplyVariant(createdPart);
			}
		}, delegate(Part createdPart)
		{
			if (updateAdaptation)
			{
				AdaptModule.UpdateAdaptation(createdPart);
			}
		});
	}

	public static Part[] CreateParts(PartSave[] partSaves, Transform holder, string sortingLayer, OnPartNotOwned onPartNotOwned, out OwnershipState[] ownershipState)
	{
		ownershipState = new OwnershipState[partSaves.Length];
		Part[] array = new Part[partSaves.Length];
		for (int i = 0; i < partSaves.Length; i++)
		{
			array[i] = CreatePart(partSaves[i], holder, sortingLayer, onPartNotOwned, out var ownershipState2);
			ownershipState[i] = ownershipState2;
		}
		return array;
	}

	public static Part CreatePart(PartSave partSave, Transform holder, string sortingLayer, OnPartNotOwned onPartNotOwned, out OwnershipState ownershipState)
	{
		if (!Base.partsLoader.parts.ContainsKey(partSave.name))
		{
			ownershipState = OwnershipState.OwnedAndUnlocked;
			return CreatePlaceholderPart();
		}
		Part createdPart = CreatePart(Base.partsLoader.parts[partSave.name], sortingLayer, delegate(Part part)
		{
			ApplySaveData(part, (false, false));
		}, LoadBurn);
		ownershipState = createdPart.GetOwnershipState();
		if (ownershipState != OwnershipState.OwnedAndUnlocked)
		{
			switch (onPartNotOwned)
			{
			case OnPartNotOwned.Allow:
				return createdPart;
			case OnPartNotOwned.UsePlaceholder:
				return HandleNotOwned(createPlaceholder: true);
			case OnPartNotOwned.Delete:
				return HandleNotOwned(createPlaceholder: false);
			}
		}
		return createdPart;
		void ApplySaveData(Part part, (bool, bool) addMissingVariables)
		{
			part.transform.parent = holder;
			part.transform.localPosition = partSave.position;
			part.orientation.orientation.Value = partSave.orientation;
			part.temperature = partSave.temperature;
			part.variablesModule.doubleVariables.LoadDictionary(partSave.NUMBER_VARIABLES, addMissingVariables);
			part.variablesModule.boolVariables.LoadDictionary(partSave.TOGGLE_VARIABLES, addMissingVariables);
			part.variablesModule.stringVariables.LoadDictionary(partSave.TEXT_VARIABLES, addMissingVariables);
		}
		Part CreatePlaceholderPart()
		{
			GameObject gameObject = new GameObject(partSave.name);
			Part part = gameObject.AddComponent<Part>();
			part.displayName = new TranslationVariable
			{
				plainText = true
			};
			part.description = new TranslationVariable
			{
				plainText = true
			};
			part.variablesModule = gameObject.AddComponent<VariablesModule>();
			part.orientation = gameObject.AddComponent<OrientationModule>();
			part.centerOfMass = new Composed_Vector2(Vector2.zero);
			part.mass = new Composed_Float("0.1");
			ApplySaveData(part, (true, true));
			part.InitializePart();
			return part;
		}
		Part HandleNotOwned(bool createPlaceholder)
		{
			UnityEngine.Object.DestroyImmediate(createdPart.gameObject);
			if (!createPlaceholder)
			{
				return null;
			}
			return CreatePlaceholderPart();
		}
		void LoadBurn(Part part)
		{
			if (partSave.burns != null)
			{
				part.burnMark = part.gameObject.AddComponent<BurnMark>();
				part.burnMark.Initialize();
				part.burnMark.burn = partSave.burns.FromSave();
				part.burnMark.ApplyEverything();
			}
		}
	}

	private static Part CreatePart(Part partPrefab, string sortingLayer, Action<Part> preInitializationSetup, Action<Part> postInitializationSetup)
	{
		Part part = UnityEngine.Object.Instantiate(partPrefab);
		try
		{
			part.name = partPrefab.name;
			preInitializationSetup(part);
			part.SetSortingLayer(sortingLayer);
			part.InitializePart();
			postInitializationSetup(part);
			return part;
		}
		catch (Exception message)
		{
			Debug.LogWarning("Error during part instantiation! [" + part.name + "]");
			Debug.LogWarning(message);
			UnityEngine.Object.Destroy(part.gameObject);
			throw;
		}
	}

	public static Part[] DuplicateParts(string sortingLayer, params Part[] parts)
	{
		OwnershipState[] ownershipState;
		return CreateParts(parts.Select((Part a) => new PartSave(a)).ToArray(), null, sortingLayer, OnPartNotOwned.Allow, out ownershipState).ToArray();
	}
}
