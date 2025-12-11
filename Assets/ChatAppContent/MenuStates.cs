using UnityEngine;
using System.Collections;

public class MenuStates : MonoBehaviour
{
    //menu states
    public enum MenuState { Main, HostMenu, ClientMenu, HostChat, ClientChat }
    public MenuState currentState;

    public GameObject mainMenuPanel;
    public GameObject HostMenuPanel;
    public GameObject ClientMenuPanel;
    public GameObject HostChatPanel;
    public GameObject ClientChatPanel;

    void Awake()
    {
        currentState = MenuState.Main;
    }

    public void OnMain()
    {
        currentState = MenuState.Main;
    }

    public void OnHostChat()
    {
        currentState = MenuState.HostMenu;
    }
    public void OnJoinChat() {
        currentState = MenuState.ClientMenu;
    }

    public void OnHost() {
        currentState = MenuState.HostChat;
    }

    public void OnJoin()
    {
        currentState = MenuState.ClientChat;
    }



    void Update()
    {
        //checks menu state
        switch (currentState)
        {
            case MenuState.Main:
                mainMenuPanel.SetActive(true);
                HostMenuPanel.SetActive(false);
                ClientMenuPanel.SetActive(false);
                HostChatPanel.SetActive(false);
                ClientChatPanel.SetActive(false);
                break;
            case MenuState.HostMenu:
                mainMenuPanel.SetActive(false);
                HostMenuPanel.SetActive(true);
                ClientMenuPanel.SetActive(false);
                HostChatPanel.SetActive(false);
                ClientChatPanel.SetActive(false);
                break;
            case MenuState.ClientMenu:
                mainMenuPanel.SetActive(false);
                HostMenuPanel.SetActive(false);
                ClientMenuPanel.SetActive(true);
                HostChatPanel.SetActive(false);
                ClientChatPanel.SetActive(false);
                break;

            case MenuState.HostChat:
                mainMenuPanel.SetActive(false);
                HostMenuPanel.SetActive(false);
                ClientMenuPanel.SetActive(false);
                HostChatPanel.SetActive(true);
                ClientChatPanel.SetActive(false);
                break;


            case MenuState.ClientChat:
                mainMenuPanel.SetActive(false);
                HostMenuPanel.SetActive(false);
                ClientMenuPanel.SetActive(false);
                HostChatPanel.SetActive(false);
                ClientChatPanel.SetActive(true);
                break;
        }

    }
}
