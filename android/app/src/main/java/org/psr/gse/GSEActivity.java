package org.psr.gse;

import android.content.Intent;
import android.net.Uri;
import android.provider.DocumentsContract;
import android.view.InputDevice;
import android.view.KeyEvent;

import java.io.File;
import java.nio.ByteBuffer;
import java.security.MessageDigest;
import java.security.SecureRandom;

import org.libsdl.app.SDLActivity;

public class GSEActivity extends SDLActivity
{
	private static final int GSE_DOCUMENT_REQUEST = 1;

	@Override
	protected String getMainFunction()
	{
		return "GSEMain";
	}

	@Override
	protected String[] getLibraries()
	{
		// SDL2 must be the first object, and GSE must be the last object
		return new String[] { "SDL2", "cimgui", "gambatte", "mgba", "native_helper", "GSE" };
	}

	@Override
	protected void onActivityResult(int requestCode, int resultCode, Intent data)
	{
		super.onActivityResult(resultCode, resultCode, data);

		if (requestCode == GSE_DOCUMENT_REQUEST)
		{
			HandleDocumentRequestResult(resultCode, resultCode, data);
		}
	}

	// Implemented C# side
	public static native void DispatchAndroidKeyEvent(int keycode, boolean pressed);

	// can't use SDLControllerManager.isDeviceSDLJoystick(), as that might false flag some keyboards
	private static boolean IsSDLJoystick(int deviceId)
	{
		var device = InputDevice.getDevice(deviceId);
		if (device == null || deviceId < 0)
		{
			return false;
		}

		// note that SOURCE_DPAD can come from keyboards!
		var sources = device.getSources();
		return (sources & InputDevice.SOURCE_CLASS_JOYSTICK) == InputDevice.SOURCE_CLASS_JOYSTICK ||
		       (sources & InputDevice.SOURCE_GAMEPAD) == InputDevice.SOURCE_GAMEPAD;
	}

	@Override
	public boolean dispatchKeyEvent(KeyEvent event)
	{
		if (SDLActivity.mBrokenLibraries)
		{
		   return false;
		}

		if (!IsSDLJoystick(event.getDeviceId()))
		{
			var action = event.getAction();
			if (action == KeyEvent.ACTION_DOWN || action == KeyEvent.ACTION_UP)
			{
				DispatchAndroidKeyEvent(event.getKeyCode(), action == KeyEvent.ACTION_DOWN);
			}
		}

		return super.dispatchKeyEvent(event);
	}

	private static String GetDisplayablePath(Uri uri)
	{
		var lastPathSegment = uri.getLastPathSegment();
		if (lastPathSegment != null)
		{
			var path = new File(lastPathSegment).getName();
			if (path.lastIndexOf('.') != -1)
			{
				return path;
			}
		}

		var projection = new String[] { DocumentsContract.Document.COLUMN_DISPLAY_NAME };
		try (var cursor = mSingleton.getContentResolver().query(uri, projection, null, null, null))
		{
			if (cursor != null)
			{
				cursor.moveToFirst();
				var path = cursor.getString(0);
				if (path.lastIndexOf('.') != -1)
				{
					return path;
				}
			}
		}
		catch (Exception ex)
		{
			System.err.println(ex.getMessage());
		}

		return null;
	}

	// Implemented C# side
	public static native void SetDocumentRequestResult(String uriAndPath);

	private static void HandleDocumentRequestResult(int requestCode, int resultCode, Intent data)
	{
		if (resultCode != RESULT_OK || data == null)
		{
			SetDocumentRequestResult(null);
			return;
		}

		var uri = data.getData();
		if (uri == null)
		{
			SetDocumentRequestResult(null);
			return;
		}

		var path = GetDisplayablePath(uri);
		if (path == null)
		{
			SetDocumentRequestResult(null);
			return;
		}

		try
		{
			mSingleton.getContentResolver().takePersistableUriPermission(uri, Intent.FLAG_GRANT_READ_URI_PERMISSION);
			var uriAndPath = uri.toString() + '|' + path;
			SetDocumentRequestResult(uriAndPath);
		}
		catch (Exception ex)
		{
			System.err.println(ex.getMessage());
			SetDocumentRequestResult(null);
		}
	}

	// Called by C# side via JNI
	public static void RequestDocument()
	{
		try
		{
			mSingleton.runOnUiThread(() ->
			{
				try
				{
					var intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
					intent.addCategory(Intent.CATEGORY_OPENABLE);
					intent.setFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);
					intent.setType("*/*");
					mSingleton.startActivityForResult(intent, GSE_DOCUMENT_REQUEST);
				}
				catch (Exception ex)
				{
					System.err.println(ex.getMessage());
					SetDocumentRequestResult(null);
				}
			});
		}
		catch (Exception ex)
		{
			System.err.println(ex.getMessage());
			SetDocumentRequestResult(null);
		}
	}

	// Called by C# side via JNI
	public static int OpenContent(String contentUri)
	{
		try
		{
			var uri = Uri.parse(contentUri);
			return mSingleton.getContentResolver().openFileDescriptor(uri, "r").detachFd();
		}
		catch (Exception ex)
		{
			System.err.println(ex.getMessage());
			return -1;
		}
	}

	// Called by C# side via JNI
	public static byte[] HashDataSHA256(ByteBuffer data)
	{
		try
		{
			var sha256 = MessageDigest.getInstance("SHA-256");
			sha256.reset();
			sha256.update(data);
			return sha256.digest();
		}
		catch (Exception ex)
		{
			System.err.println(ex.getMessage());
			return null;
		}
	}

	// Called by C# side via JNI
	public static int GetRandomInt32(int toExclusive)
	{
		var random = new SecureRandom();
		return random.nextInt(toExclusive);
	}

	// Called by C# side via JNI
	public static void OpenFileManager()
	{
		try
		{
			mSingleton.runOnUiThread(() ->
			{
				// apparently this needs to be tried against multiple methods of opening the file manager
				// reference https://github.com/dolphin-emu/dolphin/blob/401d6e70f644afd4418a93778148d53f90954d75/Source/Android/app/src/main/java/org/dolphinemu/dolphinemu/activities/UserDataActivity.kt#L115-L153

				try
				{
					var intent = new Intent(Intent.ACTION_VIEW);
					intent.addCategory(Intent.CATEGORY_DEFAULT);
					intent.setData(DocumentsContract.buildRootUri(String.format("%s.user", mSingleton.getPackageName()), DocumentProvider.ROOT_ID));
					intent.setFlags(Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION | Intent.FLAG_GRANT_PREFIX_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
					mSingleton.startActivity(intent);
					return;
				}
				catch (Exception ex)
				{
					System.err.println(ex.getMessage());
				}

				try
				{
					var intent = new Intent("android.provider.action.BROWSE");
					intent.addCategory(Intent.CATEGORY_DEFAULT);
					intent.setData(DocumentsContract.buildRootUri(String.format("%s.user", mSingleton.getPackageName()), DocumentProvider.ROOT_ID));
					intent.setFlags(Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION | Intent.FLAG_GRANT_PREFIX_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
					mSingleton.startActivity(intent);
					return;
				}
				catch (Exception ex)
				{
					System.err.println(ex.getMessage());
				}

				try
				{
					var intent = new Intent(Intent.ACTION_MAIN);
					intent.setClassName("com.google.android.documentsui", "com.android.documentsui.files.FilesActivity");
					intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
					mSingleton.startActivity(intent);
					return;
				}
				catch (Exception ex)
				{
					System.err.println(ex.getMessage());
				}

				try
				{
					var intent = new Intent(Intent.ACTION_MAIN);
					intent.setClassName("com.android.documentsui", "com.android.documentsui.files.FilesActivity");
					intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
					mSingleton.startActivity(intent);
				}
				catch (Exception ex)
				{
					System.err.println(ex.getMessage());
				}
			});
		}
		catch (Exception ex)
		{
			System.err.println(ex.getMessage());
		}
	}
}
