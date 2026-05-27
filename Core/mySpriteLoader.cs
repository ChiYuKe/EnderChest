using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace EnderChest.Core
{
	public class mySpriteLoader
	{
		private static readonly string[] SearchFolders = new string[]
		{
			Path.Combine("Assets", "Sprite"),
			Path.Combine("assets", "sprite"),
			"",
			"assets",
			"images",
			"sprites"
		};

		// Token: 0x04000030 RID: 48
		private static readonly Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();

		// Token: 0x04000031 RID: 49
		private static string modPath;
		public static void SetModPath(string path)
		{
			mySpriteLoader.modPath = path;
			mySpriteLoader.LoadedSprites.Clear();
		}

		public static Sprite GetSprite(string spriteName)
		{
			bool flag = string.IsNullOrWhiteSpace(spriteName);
			Sprite result;
			if (flag)
			{
				result = null;
			}
			else
			{
				Sprite sprite = Assets.GetSprite(spriteName);
				bool flag2 = sprite != null;
				if (flag2)
				{
					result = sprite;
				}
				else
				{
					bool flag3 = mySpriteLoader.LoadedSprites.TryGetValue(spriteName, out sprite);
					if (flag3)
					{
						result = sprite;
					}
					else
					{
						string filePath = mySpriteLoader.FindSpriteFile(spriteName);
						bool flag4 = string.IsNullOrEmpty(filePath);
						if (flag4)
						{
							result = null;
						}
						else
						{
							sprite = mySpriteLoader.LoadSpriteFromFile(spriteName, filePath);
							bool flag5 = sprite == null;
							if (flag5)
							{
								result = null;
							}
							else
							{
								mySpriteLoader.LoadedSprites[spriteName] = sprite;
								mySpriteLoader.RegisterSprite(spriteName, sprite);
								global::Debug.Log("[StorageNetwork] Registered sprite: " + spriteName + " from " + filePath);
								result = sprite;
							}
						}
					}
				}
			}
			return result;
		}

		private static string FindSpriteFile(string spriteName)
		{
			bool flag = string.IsNullOrEmpty(mySpriteLoader.modPath);
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				foreach (string folder in mySpriteLoader.SearchFolders)
				{
					string filePath = string.IsNullOrEmpty(folder) ? Path.Combine(mySpriteLoader.modPath, spriteName + ".png") : Path.Combine(mySpriteLoader.modPath, folder, spriteName + ".png");
					bool flag2 = File.Exists(filePath);
					if (flag2)
					{
						return filePath;
					}
				}
				result = null;
			}
			return result;
		}

		private static Sprite LoadSpriteFromFile(string spriteName, string filePath)
		{
			byte[] bytes = File.ReadAllBytes(filePath);
			Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
			{
				name = spriteName,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
			bool flag = !texture.LoadImage(bytes);
			Sprite result;
			if (flag)
			{
				Object.Destroy(texture);
				global::Debug.LogWarning("[StorageNetwork] Failed to decode sprite: " + filePath);
				result = null;
			}
			else
			{
				Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, (float)texture.width, (float)texture.height), new Vector2(0.5f, 0.5f), 100f, 0U, SpriteMeshType.FullRect);
				sprite.name = spriteName;
				result = sprite;
			}
			return result;
		}

		private static void RegisterSprite(string spriteName, Sprite sprite)
		{
			bool flag = Assets.Sprites == null;
			if (flag)
			{
				Assets.Sprites = new Dictionary<HashedString, Sprite>();
			}
			Assets.Sprites[new HashedString(spriteName)] = sprite;
		}
	}
}
